# K6 Load Testing

## Prerequisites

Install k6: https://k6.io/docs/getting-started/installation/

```bash
# Windows (using Chocolatey)
choco install k6

# Or download from https://k6.io/docs/getting-started/installation/
```

## Running Tests

### Load Test (Stock Quote Endpoint)

Tests the stock quote API under normal load conditions:

```bash
k6 run k6-tests/load-test.js
```

**Configuration:**
- 30s ramp-up to 10 users
- 1m ramp-up to 50 users
- 2m sustained load at 50 users
- 30s ramp-down

**Thresholds:**
- 95% of requests < 500ms
- Error rate < 10%

### Stress Test (Order Creation Endpoint)

Tests the order creation API under stress conditions:

```bash
k6 run k6-tests/stress-test.js
```

**Configuration:**
- Ramps up to 300 concurrent users
- Tests write-heavy operations (order creation)
- Identifies breaking points

**Thresholds:**
- 95% of requests < 1000ms
- Failure rate < 30%

### Custom Base URL

```bash
k6 run --env BASE_URL=https://your-api-url k6-tests/load-test.js
```

## Results

Results are saved to JSON files:
- `summary.json` (load test)
- `stress-test-summary.json` (stress test)

## Expected Performance

Based on specifications:
- Rate limiting: 10 req/sec per IP
- TWSE API timeout: 2 seconds
- Cache TTL: 5 seconds
- Response time target: < 500ms (95th percentile)
