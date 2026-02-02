import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
  stages: [
    { duration: '30s', target: 10 },  // Ramp up to 10 users
    { duration: '1m', target: 50 },   // Ramp up to 50 users
    { duration: '2m', target: 50 },   // Stay at 50 users
    { duration: '30s', target: 0 },   // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
    errors: ['rate<0.1'],             // Error rate should be below 10%
  },
};

const BASE_URL = __ENV.BASE_URL || 'https://localhost:7001';
const STOCK_CODES = ['2330', '2317', '2454', '2882', '2881'];

export default function () {
  // Select a random stock code
  const stockCode = STOCK_CODES[Math.floor(Math.random() * STOCK_CODES.length)];

  // Test 1: Get Stock Quote
  const quoteResponse = http.get(`${BASE_URL}/api/stocks/${stockCode}/quote`, {
    headers: { 'Accept': 'application/json' },
  });

  const quoteSuccess = check(quoteResponse, {
    'quote status is 200 or 503': (r) => r.status === 200 || r.status === 503,
    'quote response time < 2s': (r) => r.timings.duration < 2000,
  });

  if (!quoteSuccess) {
    errorRate.add(1);
  } else {
    errorRate.add(0);
  }

  sleep(1);

  // Test 2: Get Stock Info
  const infoResponse = http.get(`${BASE_URL}/api/stocks/${stockCode}`, {
    headers: { 'Accept': 'application/json' },
  });

  const infoSuccess = check(infoResponse, {
    'info status is 200': (r) => r.status === 200,
    'info response time < 500ms': (r) => r.timings.duration < 500,
  });

  if (!infoSuccess) {
    errorRate.add(1);
  } else {
    errorRate.add(0);
  }

  sleep(1);
}

// Summary output
export function handleSummary(data) {
  return {
    'summary.json': JSON.stringify(data),
    stdout: textSummary(data, { indent: ' ', enableColors: true }),
  };
}

function textSummary(data, options) {
  const indent = options.indent || '';
  const enableColors = options.enableColors || false;
  
  let output = '\n' + indent + '======== Load Test Summary ========\n\n';
  
  output += indent + `Total Requests: ${data.metrics.http_reqs.values.count}\n`;
  output += indent + `Request Rate: ${data.metrics.http_reqs.values.rate.toFixed(2)} req/s\n`;
  output += indent + `Average Duration: ${data.metrics.http_req_duration.values.avg.toFixed(2)} ms\n`;
  output += indent + `95th Percentile: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)} ms\n`;
  output += indent + `Error Rate: ${(data.metrics.errors.values.rate * 100).toFixed(2)}%\n`;
  
  output += '\n' + indent + '===================================\n';
  
  return output;
}
