# Lesson 2: FHIR Servers and Built-in APIs

## Overview

When you spin up a FHIR server, you're not building APIs — the server IS the API. It comes
with every endpoint pre-built for every FHIR resource type. This lesson explains what you
get out of the box and what other FHIR servers exist beyond HAPI.

---

## Step-by-Step: Setting Up HAPI FHIR Server with Docker + Postgres

### Prerequisites

Before you start, make sure you have these installed:

1. **Docker Desktop** — download from https://www.docker.com/products/docker-desktop/
   - After installing, open Docker Desktop and wait until it says "Docker is running"
   - You can verify in terminal: `docker --version`

2. **A REST client** — pick one:
   - **Postman** (recommended for beginners) — download from https://www.postman.com/downloads/
   - **VS Code REST Client** — install the "REST Client" extension in VS Code
   - **curl** — already available in your terminal

### What are we setting up?

Two Docker containers that talk to each other:

```
┌─────────────────────────────────────────────────┐
│  Docker                                         │
│                                                 │
│  ┌─────────────────┐    ┌────────────────────┐  │
│  │  fhir-server    │───>│  fhir-db           │  │
│  │  (HAPI FHIR)    │    │  (PostgreSQL 15)   │  │
│  │  Port: 8080     │    │  Port: 5433        │  │
│  │                 │    │                    │  │
│  │  Java app that  │    │  Stores all FHIR   │  │
│  │  provides the   │    │  resources as      │  │
│  │  FHIR REST API  │    │  database rows     │  │
│  └─────────────────┘    └────────────────────┘  │
│                                                 │
└─────────────────────────────────────────────────┘
         │                        │
    localhost:8080           localhost:5433
    (FHIR API + Web UI)     (DB access via pgAdmin/DBeaver)
```

- **fhir-server** — the HAPI FHIR application. Exposes the FHIR REST API on port 8080.
  This is what you'll send all your requests to.
- **fhir-db** — a PostgreSQL database that stores all the FHIR data. Exposed on port 5433
  (not 5432, to avoid clashing with any Postgres you already have installed).

### The docker-compose.yml explained

The file is at `resources/docker-compose.yml`. Here's what each part does:

```yaml
version: "3.8"

services:
  fhir-db:
    image: postgres:15                        # Use official Postgres 15 image
    environment:
      POSTGRES_DB: hapi                       # Create a database called "hapi"
      POSTGRES_USER: hapi                     # Database username
      POSTGRES_PASSWORD: hapi123              # Database password
    volumes:
      - fhir-pgdata:/var/lib/postgresql/data  # Persist data between restarts
    ports:
      - "5433:5432"                           # Map container port 5432 to host port 5433

  fhir-server:
    image: hapiproject/hapi:latest            # Use official HAPI FHIR image
    ports:
      - "8080:8080"                           # Map container port 8080 to host port 8080
    environment:
      # Tell HAPI to use Postgres instead of the default embedded H2 database
      spring.datasource.url: jdbc:postgresql://fhir-db:5432/hapi
      spring.datasource.username: hapi
      spring.datasource.password: hapi123
      spring.datasource.driverClassName: org.postgresql.Driver
      spring.jpa.properties.hibernate.dialect: org.hibernate.dialect.PostgreSQLDialect
      # Use FHIR R4 (the current standard)
      hapi.fhir.fhir_version: R4
      # Allow batch deletes (useful for testing/cleanup)
      hapi.fhir.allow_multiple_delete: "true"
      # Enable request validation against FHIR profiles
      hapi.fhir.validation.requests_enabled: "true"
    depends_on:
      - fhir-db                               # Start Postgres before HAPI

volumes:
  fhir-pgdata:                                # Named volume so data survives restarts
```

### Start the server

Open a terminal in the `Learn FHIR` folder and run:

```bash
docker compose -f resources/docker-compose.yml up -d
```

What this does:
- `-f resources/docker-compose.yml` — points to the compose file in the resources folder
- `up` — create and start the containers
- `-d` — run in the background (detached mode)

First run will take 2-5 minutes because Docker needs to download the images (~800MB total).
Subsequent starts will be fast (~10 seconds).

### Check the logs while it starts

```bash
docker compose -f resources/docker-compose.yml logs -f fhir-server
```

Watch for a line like:
```
Started Application in XX seconds
```

That means the server is ready. Press `Ctrl+C` to stop watching logs (the server keeps running).

### Useful Docker commands

| Command | What it does |
|---------|-------------|
| `docker compose -f resources/docker-compose.yml up -d` | Start the server |
| `docker compose -f resources/docker-compose.yml down` | Stop the server (keeps data) |
| `docker compose -f resources/docker-compose.yml down -v` | Stop and DELETE all data |
| `docker compose -f resources/docker-compose.yml logs -f` | Watch live logs |
| `docker compose -f resources/docker-compose.yml ps` | Check if containers are running |
| `docker compose -f resources/docker-compose.yml restart` | Restart both containers |

### Optional: Connect to the database directly

If you want to see how FHIR stores data in Postgres, connect with any SQL client:

| Setting | Value |
|---------|-------|
| Host | localhost |
| Port | 5433 |
| Database | hapi |
| Username | hapi |
| Password | hapi123 |

Use **DBeaver** (free) or **pgAdmin** to browse the tables. You'll see tables like
`hfj_resource`, `hfj_res_ver`, etc. — this is how HAPI internally stores FHIR resources.

---

## After Starting the Server — Verify and Explore

### 1. Verify it's running

Wait ~30 seconds for the server to initialize, then open your browser and go to:

```
http://localhost:8080
```

You should see the **HAPI FHIR web UI** — a dashboard where you can browse resources,
run searches, and see server info. If you see this page, your server is alive.

### 2. Check the Capability Statement

In Postman (or curl), send:

```
GET http://localhost:8080/fhir/metadata
```

This returns a big JSON document called the **CapabilityStatement**. It lists every
resource type the server supports and what operations are available. You don't need to
read the whole thing — just confirm you get a JSON response.

### 3. Try your first API call

Send this in Postman or curl to confirm the API is working:

```
GET http://localhost:8080/fhir/Patient
```

You'll get back an empty **Bundle** (FHIR's name for a list of results):

```json
{
  "resourceType": "Bundle",
  "type": "searchset",
  "total": 0,
  "entry": []
}
```

This means: "there are zero patients in the system." That's expected — you haven't
created any yet.

### 4. You're ready for the hands-on steps

Now go to `FHIR-Learning-Plan.md` and start from **Step 2** (Create a FHIR Patient).
Step 1 (server setup) is done. Each step in the plan gives you the exact JSON to send
via POST/PUT requests.

### Quick Postman setup

1. Open Postman
2. Create a new request
3. Set method to `POST`
4. Set URL to `http://localhost:8080/fhir/Patient`
5. Go to the Body tab, select "raw", choose "JSON" from the dropdown
6. Paste the Patient JSON from Step 2 of the learning plan
7. Click Send
8. You should get `201 Created` back

### If using curl instead

```bash
curl -X POST http://localhost:8080/fhir/Patient \
  -H "Content-Type: application/fhir+json" \
  -d @patient.json
```

(Where `patient.json` is a file containing the Patient JSON from Step 2)

### Troubleshooting

| Problem | Solution |
|---------|----------|
| localhost:8080 doesn't load | Wait 30-60 seconds — HAPI takes time to start on first run |
| Still doesn't load after 60s | Run `docker compose -f resources/docker-compose.yml logs fhir-server` to check for errors |
| Port 8080 already in use | Change the port in docker-compose.yml from `"8080:8080"` to `"8081:8080"`, then use port 8081 |
| Database connection errors | Run `docker compose -f resources/docker-compose.yml down -v` then `up -d` again to reset |

---

## What a FHIR Server Gives You Out of the Box

Once you run `docker compose up -d` with HAPI FHIR, you immediately have a fully functional
REST API. No code to write. No endpoints to build.

For EVERY resource type (Patient, Encounter, Observation, Condition, Procedure, and ~150 more),
you get:

| Operation | Method | URL | What it does |
|-----------|--------|-----|-------------|
| Create | POST | `/fhir/Patient` | Create a new patient |
| Read | GET | `/fhir/Patient/1` | Get patient with id 1 |
| Update | PUT | `/fhir/Patient/1` | Replace patient with id 1 |
| Delete | DELETE | `/fhir/Patient/1` | Delete patient with id 1 |
| Search | GET | `/fhir/Patient?name=Smith` | Find patients by criteria |
| History | GET | `/fhir/Patient/1/_history` | Get all versions of patient 1 |

Replace "Patient" with any resource type — Encounter, Observation, Condition, Procedure,
MedicationRequest — and it works the same way.

### Beyond basic CRUD, the server also handles:

- **Validation** — checks your JSON against FHIR rules and profiles
- **Versioning** — every update creates a new version automatically
- **Search** — rich query parameters (by name, date, code, reference, etc.)
- **References** — validates that referenced resources exist
- **Operations** — special endpoints like `$everything` (get all data for a patient)
- **History** — full audit trail of every change to every resource
- **Capability Statement** — `/fhir/metadata` tells clients what this server supports

### Your workflow is simply:

```
1. docker compose up -d          <-- start the server
2. Use Postman/curl to hit APIs  <-- create Patient, Encounter, etc.
3. Browse http://localhost:8080  <-- see your data in the HAPI web UI
```

---

## FHIR Server Landscape — All the Options

### Open Source (Free, Self-Hosted)

#### HAPI FHIR (what we're using)
- **Language:** Java
- **Database:** PostgreSQL, MySQL, Oracle, H2 (embedded)
- **FHIR Versions:** DSTU2, STU3, R4, R4B, R5
- **Why use it:** Most popular FHIR server in the world. Huge community, tons of
  documentation, full-featured. Has a built-in web UI for browsing data. The go-to
  choice for learning and for many production systems.
- **Website:** https://hapifhir.io
- **Best for:** Learning, prototyping, and production use. If you're unsure, pick this one.

#### Microsoft FHIR Server (Azure Health Data Services — OSS version)
- **Language:** C# / .NET
- **Database:** Azure SQL, Azure Cosmos DB, or SQL Server
- **FHIR Versions:** STU3, R4
- **Why use it:** If your organization is already in the Microsoft/Azure ecosystem, this
  integrates naturally. The open-source version can run locally; the managed version runs
  on Azure.
- **GitHub:** https://github.com/microsoft/fhir-server
- **Best for:** .NET shops, Azure-heavy organizations

#### IBM FHIR Server (LinuxForHealth)
- **Language:** Java
- **Database:** PostgreSQL, IBM Db2
- **FHIR Versions:** R4
- **Why use it:** Enterprise-grade, built for high performance and large datasets.
  Strong focus on conformance and spec compliance. Good multi-tenant support.
- **GitHub:** https://github.com/LinuxForHealth/FHIR
- **Best for:** Enterprise/large-scale deployments, IBM ecosystem

#### Aidbox
- **Language:** Clojure
- **Database:** PostgreSQL
- **FHIR Versions:** R4, R4B, R5
- **Why use it:** Developer-friendly, stores FHIR as native JSON in Postgres (no ORM
  overhead). Has a free community edition for development and a commercial edition for
  production. Strong focus on developer experience and custom search.
- **Website:** https://www.health-samurai.io/aidbox
- **Best for:** Teams that want direct SQL access to FHIR data in Postgres

#### Blaze
- **Language:** Clojure
- **Database:** Custom (RocksDB-based, no external DB needed)
- **FHIR Versions:** R4
- **Why use it:** Extremely fast for read-heavy workloads and CQL (Clinical Quality
  Language) queries. Self-contained — no separate database server needed.
- **GitHub:** https://github.com/samply/blaze
- **Best for:** Research, analytics, clinical quality measures

---

### Commercial / Managed (Paid)

#### Smile CDR
- **Built on:** HAPI FHIR (same core engine)
- **Language:** Java
- **Database:** PostgreSQL, Oracle, SQL Server, MySQL
- **FHIR Versions:** DSTU2, STU3, R4, R5
- **What it adds over HAPI:** Authentication/authorization (OAuth2, SMART on FHIR),
  Master Data Management (MDM/deduplication), data pipelines, HL7v2-to-FHIR conversion,
  enterprise support, admin console.
- **Website:** https://www.smilecdr.com
- **Best for:** Organizations that want HAPI in production with enterprise features
  and commercial support

#### Firely Server (formerly Vonk)
- **Language:** C# / .NET
- **Database:** SQL Server, MongoDB, SQLite
- **FHIR Versions:** STU3, R4, R5
- **What it adds:** Very strong validation and conformance testing. Great for
  organizations that need to validate FHIR profiles strictly. Also makes Simplifier.net
  (the FHIR profile registry) and the official .NET FHIR SDK.
- **Website:** https://fire.ly/products/firely-server/
- **Best for:** Profile-heavy implementations, .NET organizations, strict conformance needs

#### Google Cloud Healthcare API
- **Type:** Fully managed (no self-hosting)
- **Database:** Managed by Google
- **FHIR Versions:** DSTU2, STU3, R4
- **What it adds:** Zero infrastructure management, auto-scaling, BigQuery integration
  for analytics, de-identification for research. Part of Google Cloud.
- **Best for:** GCP-native organizations, large-scale analytics

#### Amazon HealthLake
- **Type:** Fully managed (no self-hosting)
- **Database:** Managed by AWS
- **FHIR Versions:** R4
- **What it adds:** Automatic NLP (extracts medical info from unstructured text),
  S3 integration, serverless. Part of AWS.
- **Best for:** AWS-native organizations, NLP/AI on clinical data

#### Azure Health Data Services (Managed)
- **Type:** Fully managed version of Microsoft's FHIR server
- **Database:** Managed by Azure
- **FHIR Versions:** STU3, R4
- **What it adds:** Azure AD integration, DICOM service (medical imaging), MedTech
  service (IoT device data), managed infrastructure.
- **Best for:** Azure-native organizations, integrated health data platform

---

## How to Choose

| If you... | Consider |
|-----------|---------|
| Are learning FHIR | **HAPI FHIR** (free, best docs, biggest community) |
| Need to run in production quickly | **Smile CDR** (HAPI + enterprise features) |
| Are a .NET / Azure shop | **Microsoft FHIR Server** or **Firely Server** |
| Want FHIR data queryable via SQL | **Aidbox** (native JSON in Postgres) |
| Don't want to manage infrastructure | **Google**, **AWS HealthLake**, or **Azure** managed |
| Need strict profile validation | **Firely Server** |
| Are doing research / analytics | **Blaze** (fast CQL queries) |

### For your learning journey: HAPI FHIR is the right choice

- Free, no license needed
- Runs locally in Docker — no cloud account required
- Biggest community — easiest to find help
- Web UI at `http://localhost:8080` to browse your data visually
- Postgres backing means you can inspect the actual database tables
- Same core engine as Smile CDR, so skills transfer to production

---

## Key Takeaway

A FHIR server is not a framework where you build APIs. It's a **complete, ready-to-use
data platform**. You spin it up, and every FHIR endpoint exists immediately. Your job is
to understand the data model (Resources, References, Profiles) and how to call the APIs
correctly — which is exactly what the other lessons cover.
