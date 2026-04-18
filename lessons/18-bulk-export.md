# Lesson 18: Bulk Data Operations — Exporting FHIR Data at Scale

## Overview

Throughout this course we've worked with individual resources — create a Patient, read
an Observation, search for Encounters. But real healthcare systems need to move data in
**bulk** — export all patients for analytics, extract a year of lab results for research,
or migrate an entire database to a new system.

FHIR defines a standard for this: the **Bulk Data Access** specification (also called
FHIR Bulk Data or the `$export` operation). This lesson covers the concepts and the
practical alternatives available to us.

---

## The $export Operation (Standard)

FHIR Bulk Data Access defines three export levels:

| Level | Endpoint | What It Exports |
|-------|----------|----------------|
| System | `GET /fhir/$export` | All resources on the server |
| Patient | `GET /fhir/Patient/$export` | All resources for all patients |
| Group | `GET /fhir/Group/{id}/$export` | Resources for patients in a specific group |

### How $export Works (The Async Pattern)

Bulk export is **asynchronous** — it's too much data for a single HTTP response. The
workflow is:

```
1. Client:  GET /fhir/$export
            Headers: Accept: application/fhir+json
                     Prefer: respond-async

2. Server:  202 Accepted
            Content-Location: http://server/fhir/$export-poll/job-123

3. Client:  GET /fhir/$export-poll/job-123  (poll until ready)

4. Server:  200 OK (when ready)
            {
              "output": [
                { "type": "Patient", "url": "http://server/bulk/file1.ndjson" },
                { "type": "Observation", "url": "http://server/bulk/file2.ndjson" },
                { "type": "Condition", "url": "http://server/bulk/file3.ndjson" }
              ]
            }

5. Client:  GET http://server/bulk/file1.ndjson  (download each file)
```

### NDJSON Format

Bulk exports use **NDJSON** (Newline Delimited JSON) — one JSON resource per line,
no commas between them:

```
{"resourceType":"Patient","id":"1","name":[{"family":"Smith"}]}
{"resourceType":"Patient","id":"2","name":[{"family":"Jones"}]}
{"resourceType":"Patient","id":"3","name":[{"family":"Williams"}]}
```

This format is designed for streaming — you can process one line at a time without
loading the entire file into memory. It's what tools like BigQuery, Spark, and data
pipelines expect.

### $export Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `_type` | Limit to specific resource types | `_type=Patient,Observation` |
| `_since` | Only resources modified after this date | `_since=2026-01-01T00:00:00Z` |
| `_typeFilter` | Search filters per resource type | `_typeFilter=Observation?category=vital-signs` |
| `_outputFormat` | Output format (default: ndjson) | `_outputFormat=application/ndjson` |

---

## Why HAPI Doesn't Support $export by Default

HAPI FHIR's open-source edition doesn't include the Bulk Data module out of the box.
It requires either:
- **HAPI FHIR JPA Starter** with the bulk-export module enabled
- **Smile CDR** (the commercial version of HAPI) which includes it

This is common — many FHIR servers treat bulk export as an add-on because it requires:
- Async job management (background processing)
- Large file storage (NDJSON files can be gigabytes)
- Authentication (bulk exports often use SMART Backend Services auth)

---

## Practical Alternatives We Can Use

Even without `$export`, we can do bulk data extraction using standard FHIR operations
that HAPI supports:

### 1. $everything — Patient-Level Export

The `$everything` operation returns all resources related to a patient in one request:

```
GET /fhir/Patient/{id}/$everything
```

Returns a Bundle containing the Patient plus all Encounters, Observations, Conditions,
Procedures, CarePlans, QuestionnaireResponses — everything linked to that patient.

### 2. Paginated Search — Type-Level Export

Search with pagination to extract all resources of a type:

```
GET /fhir/Patient?_count=100        → page 1 (returns next link)
GET /fhir?_getpages=xxx&_offset=100 → page 2
GET /fhir?_getpages=xxx&_offset=200 → page 3
... until no more "next" links
```

### 3. Search with _since — Incremental Export

```
GET /fhir/Patient?_lastUpdated=ge2026-04-01
```

Returns only resources modified since April 1st — useful for incremental syncs.

---

## Our Approach

We'll build a `BulkExportService` that demonstrates three practical bulk operations:

1. **PatientEverythingAsync** — uses `$everything` to get all data for one patient
2. **ExportResourceTypeAsync** — uses paginated search to extract all resources of a
   given type
3. **ExportSinceAsync** — uses `_lastUpdated` search for incremental exports

These cover the real-world use cases without needing the $export module.

---

## $everything vs. $export

| Feature | $everything | $export |
|---------|------------|---------|
| Scope | One patient | All patients / system-wide |
| Response | Synchronous Bundle | Async NDJSON files |
| Size limit | Practical limit ~1000 resources | Designed for millions |
| Server support | Most FHIR servers | Requires bulk data module |
| Use case | Patient portal, clinical view | Analytics, data warehouse |

For individual patient data, `$everything` is perfect. For population-level exports,
you need `$export` (or paginated search as a workaround).

---

## Pagination in FHIR

Search results are paginated using **Bundle links**:

```json
{
  "resourceType": "Bundle",
  "type": "searchset",
  "total": 350,
  "link": [
    { "relation": "self", "url": "http://server/fhir/Patient?_count=100" },
    { "relation": "next", "url": "http://server/fhir?_getpages=abc&_offset=100" }
  ],
  "entry": [ ... 100 patients ... ]
}
```

To get all results, follow the `next` link until there is no more `next`. The
`total` field tells you how many resources exist (though it's optional and some
servers omit it).

---

## Common Questions

**Q: How do I export data for analytics/reporting?**
Without `$export`: use paginated search per resource type, convert results to NDJSON
or CSV in your application code, load into your analytics tool. With `$export`: kick
off the async job, download the NDJSON files, load directly.

**Q: How big can a $everything response be?**
It depends on the server. HAPI returns all related resources in one Bundle, which can
get large for patients with extensive history. In practice, you might add `_count` or
date parameters to limit the scope.

**Q: Is there a way to export only specific data?**
With `$export`: use `_type` and `_typeFilter` parameters. With search: use specific
search parameters (date ranges, categories, etc.).

**Q: What about SMART Backend Services authentication?**
Production `$export` typically uses OAuth2 client credentials (SMART Backend Services
spec). The client authenticates with a JWT signed by a private key. This is for
server-to-server communication, not user-facing. Our local HAPI has no auth.

**Q: Can I use bulk export for data migration?**
Yes — this is one of the primary use cases. Export from the source system using
`$export`, transform the NDJSON if needed, then import into the target system using
Bundle transactions.

---

## Summary

- **$export** is the FHIR standard for bulk data extraction (async, NDJSON output)
- Three levels: system-wide, all patients, or a specific group
- HAPI's open-source edition doesn't include `$export` by default
- **Practical alternatives**: `$everything` (per patient), paginated search (per type),
  `_lastUpdated` search (incremental sync)
- NDJSON format: one JSON resource per line, designed for streaming and data pipelines
- Pagination follows `next` links in the Bundle until exhausted
- For population-level analytics in production, you'd enable the bulk data module or
  use a commercial FHIR server

---

## Course Complete

You've completed all 18 lessons covering the FHIR fundamentals:

| Block | Lessons | What You Learned |
|-------|---------|-----------------|
| **Foundations** | 01–03 | FHIR concepts, servers, security |
| **Core Resources** | 04–09 | Patient, Encounter, Observation, Condition, Procedure |
| **Conformance** | 10–11 | Implementation Guides, validation against US Core |
| **Patient Data** | 12–13 | Questionnaire, QuestionnaireResponse (PROMs) |
| **Care Planning** | 14–16 | PlanDefinition, ActivityDefinition, CarePlan, lifecycle management |
| **Data Operations** | 17–18 | Bundle transactions, bulk export |

### Where To Go Next

1. **Build something real** — map a dataset from your work to FHIR resources and
   load it into the HAPI server
2. **Explore more resource types** — MedicationRequest, AllergyIntolerance,
   DiagnosticReport, DocumentReference
3. **Learn SMART on FHIR** — the authorization framework for FHIR apps
4. **Try a public FHIR server** — test against real-world servers like
   `https://hapi.fhir.org/baseR4` or `https://r4.smarthealthit.org`
5. **Read the US Core IG** — understand all the profiles and constraints at
   `https://www.hl7.org/fhir/us/core/`
