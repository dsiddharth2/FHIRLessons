# Lesson 1: FHIR Resources and Encounters

## FHIR Resources — The Building Blocks

Think of a **Resource** as a **table in a database**, but standardized globally. Every piece
of healthcare data fits into a specific resource type. FHIR defines ~150 resource types, but
in practice you'll work with maybe 15-20 commonly.

The key insight: **FHIR doesn't store data in one big patient record.** It breaks clinical
data into small, independent, linked pieces. Each piece is a Resource.

### Real-world analogy

Imagine a patient's paper chart in a clinic. That chart has:
- A **cover sheet** with name, DOB, address — that's the **Patient** resource
- A **log of visits** with dates and reasons — each visit is an **Encounter**
- **Vital signs** written down during each visit — each measurement is an **Observation**
- A **problem list** of diagnosed conditions — each diagnosis is a **Condition**
- Notes about **surgeries or procedures** performed — each is a **Procedure**
- **Prescriptions** written — each is a **MedicationRequest**

FHIR just digitizes and standardizes this structure.

---

## The Most Common Resources (Grouped by Purpose)

### Who is the patient?

| Resource | What it represents | Example |
|----------|-------------------|---------|
| **Patient** | The person receiving care | John Smith, DOB 1985-07-15, MRN-001 |
| **Practitioner** | A doctor, nurse, or clinician | Dr. Jane Wilson, NPI 1234567890 |
| **Organization** | A hospital, clinic, or lab | Springfield General Hospital |

### What happened?

| Resource | What it represents | Example |
|----------|-------------------|---------|
| **Encounter** | A visit or interaction | Outpatient checkup on Apr 10 |
| **Appointment** | A scheduled future visit | Follow-up booked for May 15 |

### What was found?

| Resource | What it represents | Example |
|----------|-------------------|---------|
| **Observation** | A measurement or finding | BP 130/85, Blood glucose 95 mg/dL, Smoker: yes |
| **Condition** | A diagnosis or problem | Hypertension, Type 2 Diabetes |
| **DiagnosticReport** | A report grouping observations | Complete Blood Count report with 10 lab values |
| **AllergyIntolerance** | An allergy | Allergic to Penicillin |

### What was done?

| Resource | What it represents | Example |
|----------|-------------------|---------|
| **Procedure** | An action performed | Blood pressure monitoring, Knee replacement surgery |
| **MedicationRequest** | A prescription | Lisinopril 10mg daily for hypertension |
| **Immunization** | A vaccine given | COVID-19 vaccine, 2nd dose |

---

## Every Resource Has the Same Basic Structure

```json
{
  "resourceType": "___",        // what kind of resource
  "id": "123",                  // server-assigned unique ID
  "meta": { ... },              // version, last updated, profile
  "...fields specific to this resource type..."
}
```

The `resourceType` + `id` combo is the resource's identity. `Patient/1` means "the Patient
resource with id 1." This is how resources reference each other.

---

## Encounter — Deep Dive

### What is an Encounter?

An Encounter is a **single interaction between a patient and the healthcare system**. It's
the answer to "when and where did this happen?"

Think of it this way:
- You walk into a clinic — an Encounter starts
- The doctor checks your vitals, runs tests, gives a diagnosis — all linked to that Encounter
- You leave — the Encounter ends

**Without Encounters, you'd have a flat list of observations and diagnoses with no context
about WHEN or WHERE they happened.**

### Real-world examples of Encounters

| Scenario | Encounter class | Duration |
|----------|----------------|----------|
| You visit your doctor for a checkup | AMB (ambulatory) | 1 hour |
| You go to the ER with chest pain | EMER (emergency) | 4 hours |
| You're admitted to the hospital for surgery | IMP (inpatient) | 3 days |
| You have a video call with your doctor | VR (virtual) | 30 min |
| Your doctor calls you with lab results | VR (virtual) | 5 min |

### Encounter lifecycle (status)

```
planned  -->  in-progress  -->  finished
                           -->  cancelled
```

- **planned** — appointment is scheduled but hasn't started
- **in-progress** — patient is currently being seen
- **finished** — visit is complete
- **cancelled** — visit didn't happen

### Why Encounter matters for your migration

In your old database, you probably have something like:

```
visits table
-----------
visit_id    patient_id    visit_date    visit_type    provider    reason
1001        501           2026-04-10    Outpatient    Dr. Wilson  Checkup
```

This row becomes an **Encounter** resource. And then every other row in your database that
has `visit_id = 1001` — lab results, diagnoses, procedures — will reference `Encounter/xxx`
in FHIR.

### How Encounter connects everything

This is the critical point. Encounter is the **hub** that groups clinical events together:

```
                    Encounter/2 (Apr 10 checkup)
                         |
          +--------------+--------------+
          |              |              |
   Observation/3    Condition/4    Procedure/5
   (BP: 130/85)    (Hypertension)  (BP monitoring)
```

If the same patient comes back a month later:

```
                    Encounter/6 (May 10 follow-up)
                         |
          +--------------+--------------+
          |              |              |
   Observation/7    Condition/4      MedicationRequest/8
   (BP: 125/80)    (same condition,  (Lisinopril 10mg)
                    still active)
```

Notice that **Condition/4** (hypertension) is referenced from both encounters — the diagnosis
was made in the first visit but is still relevant in the second. The Condition resource exists
independently; encounters just link to it.

### Encounter fields explained

```json
{
  "resourceType": "Encounter",

  "status": "finished",
  // Where in the lifecycle is this visit?

  "class": { "code": "AMB" },
  // What kind of visit? Outpatient, inpatient, ER, virtual?

  "type": [{ "coding": [{ "code": "185349003", "display": "Encounter for check up" }] }],
  // More specific: what kind of outpatient visit? Checkup, follow-up, urgent?

  "subject": { "reference": "Patient/1" },
  // WHO was this visit for?

  "participant": [{ "individual": { "reference": "Practitioner/10" } }],
  // WHO provided care? (doctor, nurse, etc.)

  "period": { "start": "2026-04-10T09:00:00Z", "end": "2026-04-10T10:30:00Z" },
  // WHEN did it start and end?

  "reasonCode": [{ "coding": [{ "display": "Routine health check" }] }],
  // WHY did the patient come in?

  "location": [{ "location": { "reference": "Location/5" } }]
  // WHERE did it happen?
}
```

Encounter answers the journalist's questions: **Who, What, When, Where, Why.**

---

## The Mental Model — Three Tiers

Think of FHIR resources in three tiers:

```
Tier 1: WHO          Patient, Practitioner, Organization
                     (the people and places)

Tier 2: WHEN/WHERE   Encounter
                     (the context — groups everything that happened together)

Tier 3: WHAT         Observation, Condition, Procedure, MedicationRequest
                     (the clinical facts — always linked to Tier 1 and usually Tier 2)
```

When you migrate your old database:
1. First migrate **patients** (Tier 1)
2. Then migrate **visits** as Encounters (Tier 2)
3. Then migrate **clinical data** as Observations/Conditions/Procedures (Tier 3),
   linking each to its patient and encounter
