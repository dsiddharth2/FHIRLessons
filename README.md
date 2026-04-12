# FHIR Lessons

Hands-on lessons for learning HL7 FHIR (Fast Healthcare Interoperability Resources) — from
spinning up a local server to creating and linking clinical resources.

## What's Inside

```
FHIRLessons/
├── README.md
├── docker-compose.yml              # HAPI FHIR server + Postgres (Step 1)
├── FHIR-Learning-Plan.md           # Full step-by-step execution plan
└── lessons/
    └── 01-resources-and-encounters.md
```

## Getting Started

1. Read `FHIR-Learning-Plan.md` for the full hands-on walkthrough
2. Study the lessons in `lessons/` for deeper concept explanations
3. Run `docker compose up -d` to start your local FHIR server

## Lessons

| # | Topic | Description |
|---|-------|-------------|
| 01 | [Resources and Encounters](lessons/01-resources-and-encounters.md) | What FHIR Resources are, the common resource types, and a deep dive into Encounters |

## Prerequisites

- Docker Desktop installed and running
- A REST client (Postman, VS Code REST Client, or curl)
- Basic understanding of REST APIs and JSON
