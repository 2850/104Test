import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '5s', target: 100 },
    { duration: '10s', target: 200 },
    { duration: '5s', target: 300 },
    { duration: '10s', target: 0 },
  ],
};

const BASE_URL = 'http://localhost:5205';

export default function () {
  // Only test read operations
  const response = http.get(`${BASE_URL}/api/v1/stocks/2330/Info`);
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 1s': (r) => r.timings.duration < 1000,
  });
  
  sleep(0.1);
}

export function handleSummary(data) {
  console.log(`\n=== Read-Only Test Results ===`);
  console.log(`Total Requests: ${data.metrics.http_reqs.values.count}`);
  console.log(`Request Rate: ${data.metrics.http_reqs.values.rate.toFixed(2)} req/s`);
  console.log(`Average Duration: ${data.metrics.http_req_duration.values.avg.toFixed(2)} ms`);
  console.log(`95th Percentile: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)} ms`);
  console.log(`Failed Requests: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%`);
  console.log(`Cache Hit Expectation: Should see declining response times if cache is working`);
  
  return {
    stdout: '',
  };
}
