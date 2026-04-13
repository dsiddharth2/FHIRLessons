"""
Lesson 4: Create a FHIR Patient with US Core Profile

This script creates a Patient resource conforming to the US Core Patient profile
on a local HAPI FHIR server, then reads it back to verify.

Usage:
    pip install requests
    python create_patient_us_core.py
"""

import json
import sys

import requests

FHIR_BASE_URL = "http://localhost:8080/fhir"

PATIENT = {
    "resourceType": "Patient",
    "meta": {
        "profile": [
            "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"
        ]
    },
    "identifier": [
        {
            "system": "http://hospital.example.org/mrn",
            "value": "MRN-001",
        }
    ],
    "name": [
        {
            "use": "official",
            "family": "Smith",
            "given": ["John", "Michael"],
        }
    ],
    "gender": "male",
    "birthDate": "1985-07-15",
    "address": [
        {
            "use": "home",
            "line": ["123 Main St"],
            "city": "Anytown",
            "state": "CA",
            "postalCode": "90210",
            "country": "US",
        }
    ],
    "telecom": [
        {"system": "phone", "value": "555-123-4567", "use": "mobile"},
        {"system": "email", "value": "john.smith@example.com"},
    ],
    "extension": [
        {
            "url": "http://hl7.org/fhir/us/core/StructureDefinition/us-core-race",
            "extension": [
                {
                    "url": "ombCategory",
                    "valueCoding": {
                        "system": "urn:oid:2.16.840.1.113883.6.238",
                        "code": "2106-3",
                        "display": "White",
                    },
                },
                {"url": "text", "valueString": "White"},
            ],
        },
        {
            "url": "http://hl7.org/fhir/us/core/StructureDefinition/us-core-ethnicity",
            "extension": [
                {
                    "url": "ombCategory",
                    "valueCoding": {
                        "system": "urn:oid:2.16.840.1.113883.6.238",
                        "code": "2186-5",
                        "display": "Not Hispanic or Latino",
                    },
                },
                {"url": "text", "valueString": "Not Hispanic or Latino"},
            ],
        },
    ],
}

HEADERS = {"Content-Type": "application/fhir+json", "Accept": "application/fhir+json"}


def create_patient():
    """POST a US Core Patient to the FHIR server."""
    print("Creating US Core Patient...")
    print(f"  POST {FHIR_BASE_URL}/Patient\n")

    resp = requests.post(
        f"{FHIR_BASE_URL}/Patient",
        headers=HEADERS,
        json=PATIENT,
        timeout=30,
    )

    if resp.status_code == 201:
        patient = resp.json()
        patient_id = patient["id"]
        print(f"  Patient created successfully (201 Created)")
        print(f"  ID:        {patient_id}")
        print(f"  VersionId: {patient['meta'].get('versionId')}")
        print(f"  Updated:   {patient['meta'].get('lastUpdated')}")
        return patient_id
    else:
        print(f"  Failed with status {resp.status_code}")
        print(json.dumps(resp.json(), indent=2))
        sys.exit(1)


def read_patient(patient_id):
    """GET the Patient back from the server to verify it was stored."""
    print(f"\nReading Patient/{patient_id} back from server...")
    print(f"  GET {FHIR_BASE_URL}/Patient/{patient_id}\n")

    resp = requests.get(
        f"{FHIR_BASE_URL}/Patient/{patient_id}",
        headers={"Accept": "application/fhir+json"},
        timeout=30,
    )

    if resp.status_code == 200:
        patient = resp.json()
        name = patient["name"][0]
        full_name = f"{' '.join(name.get('given', []))} {name.get('family', '')}"
        print(f"  Name:      {full_name}")
        print(f"  Gender:    {patient.get('gender')}")
        print(f"  BirthDate: {patient.get('birthDate')}")
        print(f"  MRN:       {patient['identifier'][0]['value']}")
        print(f"\nFull resource:\n{json.dumps(patient, indent=2)}")
    else:
        print(f"  Failed with status {resp.status_code}")
        print(json.dumps(resp.json(), indent=2))
        sys.exit(1)


if __name__ == "__main__":
    pid = create_patient()
    read_patient(pid)
    print(f"\nSave this reference for future lessons: Patient/{pid}")
