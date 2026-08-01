---
name: documentation-maintenance
description: Maintain MiniCore Markdown documentation consistently. Use whenever creating or editing files in Docs/, adding dated test or optimization records, restructuring a document, or updating cross-document links.
---

# Documentation Maintenance

Keep user-facing documents readable, source-backed, and internally consistent. Treat [Docs/DocumentationConventions.md](../../../Docs/DocumentationConventions.md) as the project standard.

## Workflow

1. Read the target document and identify whether the edited area is conceptual guidance, a procedure, or a chronological record.
2. For chronological records, inspect all sibling dated headings before editing. Keep the newest record at the top and insert each new record above older ones.
3. Do not reverse numbered procedures, architecture layers, protocol layouts, or causal sequences; the ordering rule applies only to historical records.
4. When a code or test conclusion changes, update the owning design document and its test or optimization record in the same task.
5. Keep report directory timestamps distinct from local calendar time; state UTC explicitly when it matters.
6. After editing, verify Markdown structure, links, and dated-heading order with targeted searches. Preserve existing content unless the task explicitly supersedes it.

## Required checks

- Use dates in the form `YYYY-MM-DD` for new historical headings.
- For same-day records, place later events above earlier events and include enough title detail to distinguish them.
- Keep a newer record from contradicting an older one silently: state whether it supersedes, narrows, or invalidates the earlier conclusion.
- Add a link to a new foundational document from an existing reading map when it changes project-wide working rules.
