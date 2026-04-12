# Lesson 3: FHIR Security — Why Our Server Has No Auth (and What Real Systems Do)

## Overview

Our local HAPI FHIR server has no authentication or authorization. Anyone who can reach
`http://localhost:8080` can do anything — read all patients, delete records, modify data.
This is intentional for learning, but you need to understand what production FHIR systems
require.

---

## Why Our Server Has No Security

HAPI FHIR (the open-source version) is a **data engine**, not a complete platform. It
doesn't include authentication out of the box. This is by design — it lets you:

- Learn the FHIR data model without fighting with tokens and permissions
- Focus on understanding Resources, References, and Profiles first
- Run locally where security isn't a concern (only you can reach localhost)

**This is acceptable because:**
- It's running on your laptop, not exposed to the internet
- There's no real patient data — just test data you create
- It's a learning environment, not production

**This would be unacceptable in production because:**
- Patient health data is protected by law (HIPAA in the US)
- Any breach of medical records is a serious legal and ethical issue
- Unauthorized access could lead to incorrect treatment decisions

---

## What Real FHIR Systems Use: SMART on FHIR

The standard security framework for FHIR is called **SMART on FHIR** (Substitutable
Medical Applications, Reusable Technologies). It's built on top of OAuth 2.0.

### How it works (simplified)

Without SMART (our current setup):
```
Client (Postman)  --->  FHIR Server
   "Give me Patient/1"     "Here you go!"
```

With SMART (production):
```
Client (App)  --->  Authorization Server  --->  FHIR Server
   "I need access"     "Log in, pick patient,     "Here's Patient/1,
                        here's a token"             your token checks out"
```

### The flow step by step

```
1. App requests authorization
        |
        v
2. User logs in (username/password, SSO, etc.)
        |
        v
3. User selects which patient's data the app can access
        |
        v
4. Authorization server issues an ACCESS TOKEN
   (a JWT or opaque token with scopes like "patient/Patient.read")
        |
        v
5. App sends requests to FHIR server WITH the token
   GET /fhir/Patient/1
   Authorization: Bearer eyJhbGciOiJS...
        |
        v
6. FHIR server validates the token and checks:
   - Is this token still valid (not expired)?
   - Does this token have permission to read Patient resources?
   - Does this token have access to THIS specific patient?
        |
        v
7. If all checks pass → return the data
   If any check fails → return 401 Unauthorized or 403 Forbidden
```

### SMART Scopes — What Permissions Look Like

SMART uses **scopes** to define what an app can do. They follow a pattern:

```
<context>/<resource>.<permission>
```

| Scope | Meaning |
|-------|---------|
| `patient/Patient.read` | Read the current patient's demographics |
| `patient/Observation.read` | Read the current patient's observations |
| `patient/Condition.read` | Read the current patient's conditions |
| `patient/*.read` | Read all resource types for the current patient |
| `user/Patient.read` | Read any patient (based on user's role) |
| `user/Patient.write` | Create/update any patient |
| `user/*.read` | Read everything (admin/clinician level) |
| `system/Patient.read` | Backend service reads patients (no user context) |

**"patient/" scopes** = the app can only see ONE patient's data (the one the user selected).
This is what most patient-facing apps use.

**"user/" scopes** = the app can see whatever the logged-in user's role allows. This is what
clinician-facing apps use (a doctor can see multiple patients).

**"system/" scopes** = backend service-to-service access with no user involved. Used for
data pipelines, bulk exports, integrations.

### Example: What a request looks like with SMART

Without auth (what we do now):
```http
GET http://localhost:8080/fhir/Patient/1
Content-Type: application/fhir+json
```

With SMART auth (production):
```http
GET https://fhir.hospital.org/fhir/Patient/1
Content-Type: application/fhir+json
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

The only difference is the `Authorization` header with a Bearer token.

---

## Other Security Layers in Production FHIR Systems

SMART on FHIR handles app-level authorization, but production systems have more layers:

### 1. Transport Security (TLS/HTTPS)
```
http://localhost:8080/fhir/Patient     <-- what we use (unencrypted, local only)
https://fhir.hospital.org/fhir/Patient <-- production (encrypted in transit)
```
All production FHIR servers use HTTPS. Data in transit is encrypted.

### 2. Authentication (Who are you?)
- **For users:** Username/password, SSO (Active Directory, Okta, Azure AD), MFA
- **For apps/services:** Client credentials (client ID + secret), signed JWTs, certificates

### 3. Authorization (What can you do?)
- **SMART scopes** — define what resources and operations are allowed
- **Role-Based Access Control (RBAC)** — doctors see more than billing staff
- **Consent management** — patient can restrict who sees their data

### 4. Audit Logging
Every access to patient data is logged:
```
WHO accessed WHAT data, WHEN, from WHERE, and WHY
```
FHIR has a resource for this: **AuditEvent**. Production systems create an AuditEvent
for every API call automatically.

### 5. Data at Rest Encryption
The database itself is encrypted. Even if someone steals the hard drive, they can't
read the data without the encryption keys.

### The full security stack:

```
┌────────────────────────────────┐
│  TLS/HTTPS (encrypted transit) │  ← Layer 1: Can't sniff the wire
├────────────────────────────────┤
│  Authentication (who are you?) │  ← Layer 2: Prove your identity
├────────────────────────────────┤
│  SMART Scopes (what can you?)  │  ← Layer 3: Check your permissions
├────────────────────────────────┤
│  Consent (patient preferences) │  ← Layer 4: Patient said no? Blocked.
├────────────────────────────────┤
│  Audit Log (who did what)      │  ← Layer 5: Everything is recorded
├────────────────────────────────┤
│  Encryption at Rest (DB level) │  ← Layer 6: Stolen disk is useless
└────────────────────────────────┘
```

---

## Which FHIR Servers Support Security?

| Server | Built-in Auth? | SMART on FHIR? | Notes |
|--------|---------------|----------------|-------|
| **HAPI FHIR** (open source) | No | No | You add your own auth layer (e.g., reverse proxy, interceptors) |
| **Smile CDR** (commercial HAPI) | Yes | Yes | Full SMART on FHIR, OAuth2, RBAC, consent management |
| **Firely Server** | Yes | Yes | Built-in SMART support |
| **Google Healthcare API** | Yes | Yes | Google Cloud IAM + SMART |
| **Azure Health Data Services** | Yes | Yes | Azure AD + SMART |
| **Amazon HealthLake** | Yes | Partial | AWS IAM, limited SMART support |
| **Aidbox** | Yes | Yes | Built-in auth and access policies |

This is one of the main reasons organizations pay for **Smile CDR** instead of using
free HAPI FHIR — you get security, consent management, and audit logging built in.

---

## How Would You Add Security to Our HAPI Server?

For learning purposes, you don't need to. But if you wanted to, there are a few approaches:

### Option 1: HAPI Interceptors (Code-level)
HAPI has an interceptor framework where you can write Java code that runs before every
request. You could validate tokens, check permissions, etc. Requires Java coding.

### Option 2: Reverse Proxy (Infrastructure-level)
Put something like **nginx** or **Keycloak** in front of HAPI:
```
Client  --->  Keycloak (auth)  --->  nginx (proxy)  --->  HAPI FHIR
```
Keycloak handles login and tokens, nginx forwards authenticated requests to HAPI.

### Option 3: Use Smile CDR instead
Drop-in replacement for HAPI with auth built in. Same FHIR API, but with a security
layer, admin console, and more.

---

## HIPAA and FHIR — The Legal Side

In the US, patient health data is protected by **HIPAA** (Health Insurance Portability
and Accountability Act). Key rules:

| Rule | What it means for FHIR |
|------|----------------------|
| **Privacy Rule** | Only authorized people/apps can access patient data |
| **Security Rule** | Technical safeguards required (encryption, access controls, audit logs) |
| **Breach Notification** | If data is leaked, you must notify patients within 60 days |
| **Minimum Necessary** | Only request/return the minimum data needed for the task |

A production FHIR server must comply with all of these. Our localhost setup doesn't
need to — there's no real patient data.

---

## Key Takeaways

- **Our local server has no security — and that's OK for learning.** It's localhost with
  test data.
- **Production FHIR systems use SMART on FHIR** — OAuth 2.0-based authorization with
  scopes that control access at the resource level.
- **Security is multi-layered** — HTTPS, authentication, authorization (SMART), consent,
  audit logging, encryption at rest.
- **HAPI FHIR (free) has no built-in auth.** You either add it yourself or use a
  commercial version like Smile CDR.
- **HIPAA compliance** requires all these layers for any system handling real patient data
  in the US.
- **For your migration project**, security will be a critical piece to plan — but you can
  learn the data model first and add security later.
