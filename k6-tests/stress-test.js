import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Stress test configuration
export const options = {
  stages: [
    { duration: '5s', target: 100 },   // Ramp up to 100 users
    { duration: '5s', target: 200 },   // Ramp up to 200 users
    { duration: '10s', target: 200 },  // Stay at 200 users (stress period)
    { duration: '3s', target: 300 },   // Spike to 300 users
    { duration: '5s', target: 300 },   // Stay at 300 users (breaking point)
    { duration: '2s', target: 0 },     // Ramp down to 0 users
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'], // 95% of requests should be below 1s
    http_req_failed: ['rate<0.3'],     // Failure rate should be below 30%
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5205';
const STOCK_CODES = ['2330']; // Focus on 2330 only to avoid price limit issues

export default function () {
  const stockCode = STOCK_CODES[Math.floor(Math.random() * STOCK_CODES.length)];
  const userId = Math.floor(Math.random() * 1000) + 1;

  // Test 1: Create Order (write-heavy operation)
  const orderPayload = JSON.stringify({
    userId: userId,
    stockCode: stockCode,
    orderType: Math.random() > 0.5 ? 1 : 2, // Random buy/sell
    buySell: Math.random() > 0.5 ? 1 : 2, // 1=Buy, 2=Sell
    price: 1700.0 + Math.random() * 200, // Price range 1700-1900 (within 2330's limit 1590-1940)
    quantity: (Math.floor(Math.random() * 10) + 1) * 1000, // 1000-10000
  });

  const orderResponse = http.post(`${BASE_URL}/api/v1/orders`, orderPayload, {
    headers: { 
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    },
  });

  const orderSuccess = check(orderResponse, {
    'order status is 201 or 400 or 404': (r) => [201, 400, 404].includes(r.status),
    'order response time < 3s': (r) => r.timings.duration < 3000,
  });

  if (!orderSuccess) {
    errorRate.add(1);
  } else {
    errorRate.add(0);
  }

  sleep(0.5);

  // Test 2: Read operations (quote + info)
  const quoteResponse = http.get(`${BASE_URL}/api/v1/stocks/${stockCode}/Info`);
  
  check(quoteResponse, {
    'quote response is valid': (r) => r.status === 200 || r.status === 503,
  });

  sleep(0.5);
}

export function handleSummary(data) {
  return {
    'stress-test-summary.json': JSON.stringify(data),
    stdout: textSummary(data),
  };
}

function textSummary(data) {
  let output = '\n======== Stress Test Summary ========\n\n';
  
  output += `Total Requests: ${data.metrics.http_reqs.values.count}\n`;
  output += `Request Rate: ${data.metrics.http_reqs.values.rate.toFixed(2)} req/s\n`;
  output += `Average Duration: ${data.metrics.http_req_duration.values.avg.toFixed(2)} ms\n`;
  output += `95th Percentile: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)} ms\n`;
  output += `Max Duration: ${data.metrics.http_req_duration.values.max.toFixed(2)} ms\n`;
  output += `Failed Requests: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%\n`;
  
  output += '\n=====================================\n';
  
  return output;
}
