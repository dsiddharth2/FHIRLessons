# FHIR Lessons

Hands-on lessons for learning HL7 FHIR (Fast Healthcare Interoperability Resources) — from
spinning up a local server to creating and linking clinical resources.

## What's Inside

```
FHIRLessons/
├── README.md
├── resources/
│   └── docker-compose.yml          # HAPI FHIR server + Postgres (Step 1)
├── FHIR-Learning-Plan.md           # Full step-by-step execution plan
└── lessons/
    └── 01-resources-and-encounters.md
```

## Getting Started

1. Read `FHIR-Learning-Plan.md` for the full hands-on walkthrough
2. Study the lessons in `lessons/` for deeper concept explanations
3. Run `docker compose -f resources/docker-compose.yml up -d` to start your local FHIR server

## Lessons

| # | Topic | Description |
|---|-------|-------------|
| 01 | [Resources and Encounters](lessons/01-resources-and-encounters.md) | What FHIR Resources are, the common resource types, and a deep dive into Encounters |
| 02 | [FHIR Servers and APIs](lessons/02-fhir-servers-and-apis.md) | What a FHIR server gives you out of the box, and all the server options available |
| 03 | [FHIR Security and SMART](lessons/03-fhir-security-and-smart.md) | Why our server has no auth, SMART on FHIR, OAuth scopes, and production security layers |
| 04 | [Create Patient (US Core)](lessons/04-create-patient-us-core.md) | Patient resource deep dive, every field explained, US Core profile, extensions, and migration mapping |
| 05 | [Update Patient Demographics](lessons/05-update-patient-demographics.md) | PUT vs PATCH, versioning, _history, CodeableConcept pattern, maritalStatus |

## Prerequisites

- Docker Desktop installed and running
- A REST client (Postman, VS Code REST Client, or curl)
- Basic understanding of REST APIs and JSON
