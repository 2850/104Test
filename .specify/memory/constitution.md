<!--
Sync Impact Report:
- Version change: 1.2.0 → 1.3.0
- Added sections: New Principle VII (Third-Party Package Stability)
- Modified principles: None
- Removed sections: None
- Templates requiring updates: ✅ updated
- Follow-up TODOs: None
-->

# OBBPM Constitution

## Core Principles

### I. Code Quality Excellence (NON-NEGOTIABLE)
Every code contribution MUST meet the following standards:
- **Clarity**: Code must be self-documenting with meaningful variable names, clear function signatures, and minimal cognitive load
- **Consistency**: Follow established patterns within the codebase; use linting tools (ESLint, Prettier, Black, etc.) with zero tolerance for violations
- **Maintainability**: Write code that can be easily modified, extended, and debugged by team members
- **Documentation**: Public APIs, complex algorithms, and business logic MUST have comprehensive documentation

**Rationale**: High code quality reduces technical debt, improves team velocity, and ensures long-term project sustainability.

### II. Behavior-Driven Development & Testing (NON-NEGOTIABLE)
Development MUST follow BDD principles with comprehensive testing foundation:
- **BDD Approach**: All features MUST be developed using Behavior-Driven Development methodology with Given-When-Then scenarios
- **Living Documentation**: Feature specifications MUST include executable scenarios that serve as both documentation and acceptance tests
- **Test-First Implementation**: Write failing tests before implementation; Red-Green-Refactor cycle strictly enforced
- **Coverage Requirements**: Minimum 90% code coverage for all production code; 100% for critical business logic
- **Test Types**: Unit tests (fast, isolated), integration tests (contract verification), end-to-end tests (user journey validation), behavior tests (scenario validation)
- **Quality Gates**: All tests must pass before code review; no exceptions for "quick fixes"

**Rationale**: BDD ensures requirements are clearly understood by all stakeholders, prevents miscommunication, and creates executable documentation that stays current with implementation.

### III. User Experience Consistency
User-facing features must provide cohesive, predictable experiences:
- **Design System Compliance**: All UI components MUST follow the established design system; no custom styling without approval
- **Accessibility Standards**: WCAG 2.1 AA compliance mandatory; screen reader support, keyboard navigation, color contrast requirements
- **Cross-Platform Consistency**: Functionality and behavior must be identical across all supported platforms and browsers
- **Error Handling**: User-friendly error messages with clear next steps; no technical jargon exposed to end users

**Rationale**: Consistent UX reduces user confusion, improves adoption rates, and builds trust in the platform.

### IV. Performance Excellence
Performance is a feature, not an afterthought:
- **Response Time Targets**: API responses <200ms p95, page loads <2s, interactive elements <100ms
- **Resource Efficiency**: Memory usage <100MB per user session, efficient database queries with proper indexing
- **Scalability Planning**: Code must handle 10x current load; horizontal scaling patterns preferred
- **Performance Monitoring**: Real-time performance metrics tracked; alerts for SLA violations

**Rationale**: Performance directly impacts user satisfaction, conversion rates, and operational costs.

### V. Documentation Localization (NON-NEGOTIABLE)
All user-facing and project documentation MUST be written in Traditional Chinese (zh-TW):
- **Specifications**: Feature specifications (spec.md) MUST be written in Traditional Chinese
- **Implementation Plans**: Implementation plans (plan.md) MUST be written in Traditional Chinese
- **User Documentation**: All user-facing documentation including guides, help text, and error messages MUST be in Traditional Chinese
- **Task Lists**: Task descriptions and user-facing content in tasks.md MUST be in Traditional Chinese
- **Code Comments**: User-facing code comments and documentation strings MUST be in Traditional Chinese
- **Exception**: Internal development documentation (this constitution, technical configurations, code-level comments for developers) may remain in English

**Rationale**: Ensures accessibility for Traditional Chinese speaking users and stakeholders, maintains consistency in user-facing content, and aligns with target market requirements.

### VI. MVP First Development (NON-NEGOTIABLE)
All development MUST follow Minimum Viable Product principles:
- **MVP Planning**: Every feature MUST be broken down into the smallest viable increment that delivers user value
- **Incremental Delivery**: Features MUST be delivered in independent, testable increments rather than large batches
- **User Story Prioritization**: User stories MUST be prioritized by value delivery; implement highest value stories first
- **Early Validation**: MVP implementations MUST include mechanisms for early user feedback and validation
- **Iterative Enhancement**: Features MUST be enhanced iteratively based on user feedback rather than upfront speculation
- **Scope Control**: Prevent feature creep by maintaining strict focus on core MVP functionality before adding enhancements

**Rationale**: MVP approach reduces time to market, minimizes waste, enables early user feedback, and ensures resources focus on features that deliver real user value.

### VII. Third-Party Package Stability (NON-NEGOTIABLE)

Third-party packages and dependencies MUST NOT be modified or forked without exceptional justification:

- **Package Integrity**: Third-party packages MUST be used as-is from official repositories (NuGet, NPM, etc.)
- **No Direct Modifications**: Source code of third-party packages MUST NOT be modified directly
- **No Unofficial Forks**: Using unofficial forks or modified versions of packages is PROHIBITED
- **Version Pinning**: Package versions MUST be explicitly pinned to prevent unexpected breaking changes
- **Security Updates**: Security updates MUST be applied promptly but through official package channels only
- **Alternative Solutions**: If a package limitation is encountered, explore configuration options, wrapper patterns, or alternative packages before considering modifications
- **Exception Process**: Any modification or fork requires written justification, security review, and senior architect approval with documented maintenance plan

**Rationale**: Maintaining package integrity ensures security, supportability, predictable updates, and reduces technical debt from maintaining custom modifications.

## Performance Standards

### Frontend Performance

- **Bundle Size**: JavaScript bundles <250KB gzipped; lazy loading for non-critical features
- **Rendering**: First Contentful Paint <1.5s, Largest Contentful Paint <2.5s
- **Interactivity**: Time to Interactive <3s on 3G networks
- **Optimization**: Image optimization, CDN usage, critical CSS inlining

### Backend Performance

- **Database**: Query execution <50ms p95; proper indexing and query optimization
- **API Design**: RESTful principles, efficient pagination, proper HTTP caching headers
- **Resource Management**: Connection pooling, proper cleanup of resources
- **Monitoring**: APM tools for real-time performance tracking

## Quality Assurance

### Code Review Process

All code changes require:

- **Peer Review**: Minimum one approval from senior team member
- **Constitution Compliance**: Reviewer must verify adherence to all principles
- **Performance Impact**: Assessment of performance implications for all changes
- **Documentation Review**: Ensure documentation is updated alongside code changes

### Continuous Integration

- **Automated Testing**: All tests run on every commit; build fails on test failures
- **Quality Checks**: Linting, security scanning, dependency vulnerability checks
- **Performance Regression**: Automated performance testing prevents regressions
- **Deployment Gates**: Manual approval required for production deployments

### Security Standards

- **Input Validation**: All user inputs sanitized and validated
- **Authentication**: Multi-factor authentication for admin access
- **Authorization**: Role-based access control with principle of least privilege
- **Data Protection**: Encryption at rest and in transit; PII handling compliance

## Governance

This constitution supersedes all other development practices and policies. All team members are responsible for understanding and adhering to these principles.

### Amendment Process

1. **Proposal**: Constitution changes must be documented with clear rationale
2. **Review**: Team discussion and impact assessment required
3. **Approval**: Unanimous approval from senior team members
4. **Migration**: Clear migration plan for existing code that violates new principles
5. **Version Control**: Semantic versioning for constitution changes

### Compliance Enforcement

- **Pre-commit Hooks**: Automated checks prevent non-compliant code from entering repository
- **Regular Audits**: Monthly reviews of codebase compliance with constitution principles
- **Training**: Team members receive training on constitution updates
- **Escalation**: Non-compliance issues escalated through defined management chain

### Exception Handling

Rare exceptions to constitutional principles require:

- **Documentation**: Clear justification for why standard principles cannot be followed
- **Time-boxed**: Explicit timeline for bringing code into compliance
- **Technical Debt**: Tracked as technical debt with assigned owner and remediation plan
- **Senior Approval**: Sign-off from tech lead and product owner

DO NOT Create a new markdown file to document each change or summarize your work unless specifically requested by the user.

**Version**: 1.3.0 | **Ratified**: 2025-11-05 | **Last Amended**: 2025-11-07
