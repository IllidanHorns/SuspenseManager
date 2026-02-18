# SuspenseManager — Project Guide

## What This System Does

SuspenseManager automates handling of **invalid records from streaming platform reports** (Yandex Music, Apple Music, Spotify, YouTube Music). These records — called "suspenses" — failed automatic validation and require manual processing before royalties can be distributed correctly.

A suspense fails validation for exactly one of two reasons:
- **No Product (нет продукта)**: the music product (track/album) referenced in the streaming report doesn't exist in the company's catalog
- **No Rights (нет прав)**: the product exists in the catalog, but there's no rights record defining who should receive royalty payments

These two reasons create **two parallel processing branches that never intersect**.

## Core Business Flow

```
Streaming Report Upload
        |
   Validation
    /    |    \
   v     v     v
Status 0   Status 1   Status 88
(no product) (no rights) (validated - success)
   |           |
   v           v
Grouping    Grouping
   |           |
   v           v
Status 15   Status 16
(grouped)   (grouped)
   |           |
   v           v
Processing  Processing
   |           |
  ...         ...
```

### Validation (Upload)

1. User uploads Excel report or enters data manually
2. System validates format (ISRC, barcode, required fields)
3. **Product search** in catalog (priority: ISRC > barcode > title+artist > catalog number)
4. If product NOT found → suspense with status **0** (no product)
5. If product found → **rights search** (company, contract, territory, period)
6. If rights NOT found → suspense with status **1** (no rights), linked to found product
7. If rights found → status **88** (successful stream, goes to royalty accounting)

### Grouping (Dynamic)

Suspenses are grouped dynamically for batch processing. **Critical rule: cannot mix statuses 0 and 1 in one group.**

#### Two-step process: Preview → Commit

**Step 1 — Preview** (`GET /api/grouping/preview`):
- User selects status (0 or 1) and a set of columns to group by
- System performs dynamic `GROUP BY` and returns aggregated results with `COUNT` per group
- Supports dynamic filtering (suffix operators: `_contains`, `_gt`, `_lt`, `_gte`, `_lte`, `_from`, `_to`)
- Supports sorting by any grouping column or by `Count`
- Supports pagination (`PageNumber`, `PageSize`)

**Step 2 — Commit** (`POST /api/grouping/commit`):
- User selects a specific group from preview (sends `KeyValues` matching the group)
- System finds all matching suspenses via `WHERE` on the same criteria
- Creates `SuspenseGroup`, updates statuses (0→15, 1→16), creates `SuspenseGroupLink` for each line
- One group committed at a time (no batch)

#### Allowed Grouping Columns

**Status 0 (NoProduct) — all from SuspenseLine:**
- Isrc, Barcode, CatalogNumber, Artist, TrackTitle, Genre
- SenderCompany, RecipientCompany, Operator
- AgreementType, AgreementNumber, TerritoryCode

**Status 1 (NoRights) — product fields from CatalogProduct, rest from SuspenseLine:**
- **ProductId** (MANDATORY — always included in grouping)
- From CatalogProduct: Isrc, Barcode, CatalogNumber, ProductName, Artist
- From SuspenseLine: SenderCompany, RecipientCompany, Operator, AgreementType, AgreementNumber, TerritoryCode

#### Technical Implementation

- **Raw SQL** with parameterized queries for dynamic `GROUP BY` (EF Core doesn't support dynamic column sets)
- **Whitelist of columns** mapped to SQL expressions — prevents SQL injection (`GroupingSqlBuilder`)
- For status 1: `INNER JOIN CatalogProducts` to source product fields from catalog
- Commit uses **EF Core** with standard `Where()` + transaction (create group → update lines → create links)
- Key files:
  - `Application/Helpers/GroupingSqlBuilder.cs` — SQL query builder with column whitelist
  - `Application/Services/GroupingService.cs` — PreviewAsync + CommitAsync
  - `Application/Interfaces/IGroupingService.cs` — interface
  - `Common/DTOs/GroupingDtos.cs` — GroupingPreviewRequest, GroupingPreviewItem, GroupingCommitRequest
  - `Presentation/Controllers/GroupingController.cs` — API endpoints

### Group Processing — No Product (Status 15)

| Action | Result |
|--------|--------|
| **Quick Cataloging** | Create new product in catalog → group moves to status 16 (now needs rights) |
| **Possible Products** | Search catalog for similar products (fuzzy match) → link to existing product → status 16 |
| **Send to Back Office** | status → 120 |
| **Postpone** | status → 30 |
| **Ungroup** | Archive group, suspenses return to status 0, GroupId nulled |
| **Export** | Excel export of group's suspenses |

### Group Processing — No Rights (Status 16)

| Action | Result |
|--------|--------|
| **Rights Correction** | Define/update rights metadata for the group |
| **Send to Back Office** | status → 320 |
| **Postpone** | status → 32 |
| **Ungroup** | Archive group, suspenses return to status 1, **ProductId preserved** |
| **Export** | Excel export of group's suspenses |

### Postponed Groups

Groups set aside for later. Can be returned to active processing (30→15, 32→16).

### Back Office

Complex cases for specialist review (status 120 or 320). Can be completed, returned to operator, or archived.

## Status Codes (BusinessStatus enum)

| Code | Name | Meaning |
|------|------|---------|
| 0 | NoProduct | Suspense not in group, no product found |
| 1 | NoRights | Suspense not in group, no rights found |
| 15 | InGroupNoProduct | In group, no product |
| 16 | InGroupNoRights | In group, no rights |
| 30 | PostponedNoProduct | Postponed, no product |
| 32 | PostponedNoRights | Postponed, no rights |
| 88 | Validated / SuccessfulStream | Successfully validated |
| 120 | BackOfficeNoProduct | In back office, no product |
| 320 | BackOfficeNoRights | In back office, no rights |

## Metadata Concept

Metadata are **group-level overrides** that apply to all suspenses in the group at read time, without modifying the original suspense records.

- **GroupMetadata** — product fields (title, artist, ISRC, barcode, TTkey, etc.)
- **GroupMetaRights** — rights fields (companies, contract, territory, period, share)

Priority: metadata value > suspense value. If metadata field is null, fall back to suspense value.

**Critical: metadata exists and is editable independently of product linkage.** GroupMetadata can be created/updated at any time for any group — it's simply "shared fields for all suspenses in the group". The operator can override title, artist, ISRC, etc. through metadata regardless of whether a product is linked.

**However**, when `CatalogProductId` is set in GroupMetadata — this acts as a **trigger**: the group becomes linked to that catalog product, and the group transitions to status "no rights" (15→16). Setting the product ID in metadata is what connects the group to a product and changes the processing branch.

So `GroupMetadata.CatalogProductId` must be **nullable** — metadata can exist without a product link.

**Important**: For status 16 (no rights), product data always comes from the **catalog product**, not from suspenses.

## Project Structure

```
SuspenseManager.sln
├── SuspenseManager.Models        — Domain entities, enums
├── SuspenseManager.Data          — EF Core DbContext, configurations, migrations, seeder
├── SuspenseManager.Application   — Business logic services, interfaces, helpers
│   ├── Interfaces/               — Service interfaces (IGroupingService, IGroupService, etc.)
│   ├── Services/                 — Service implementations
│   ├── Helpers/                  — Utilities (GroupingSqlBuilder — SQL builder for dynamic GROUP BY)
│   └── Validators/               — FluentValidation validators
├── SuspenseManager.Common        — Shared DTOs, exceptions, extensions
├── SuspenseManager.Presentation  — ASP.NET Core Web API, controllers, middleware
└── SuspenseManager.Tools         — Test data generator utility
```

## Tech Stack

- .NET 9, ASP.NET Core Web API, C# 13
- EF Core 9 + SQL Server (local: PAKHTUSOV-NB\SQLEXPRESS)
- ClosedXML for Excel parsing
- Docker (Linux) support
- Swagger/OpenAPI

## Key Entities & Relationships

- **SuspenseLine** — individual suspense record from a streaming report
  - Has `GroupId` (FK, nullable) — active group membership
  - Has `ProductId` (FK, nullable) — linked catalog product
  - Has `SenderCompanyId`, `RecipientCompanyId` (FK, nullable)
- **SuspenseGroup** — batch of suspenses grouped for processing
  - Has `CatalogProductId` (FK, nullable) — linked product (set after catalog-fast or link-product)
  - Has `MetaDataId` → GroupMetadata, `MetaRightsId` → GroupMetaRights
  - Has `AccountId` — who created the group
- **SuspenseGroupLink** — history of group memberships (preserved after ungrouping)
- **CatalogProduct** — music product in the catalog (ISRC, barcode, title, artist, etc.)
  - Has `ProductTypeId` → CatalogProductType (CD, VINYL, DIGI, CASS)
- **CatalogProductRights** — rights records for a product (sender/receiver companies, contract, territory, period, share)
- **Company** — company entity (rights holders, distributors, platforms)
- **Territory** — geographic territory code (RU, US, GB, etc.)
- **Account** — user account (login, password hash)
  - 1:1 with **User** (personal info: name, email, phone, position)
  - M:M with **Rights** through **AccountRightsLink** (permission-based access)
- **Rights** — permission definition (code like "uploads.view", "groups.no_product.catalog_fast")
- **GroupMetadata** — product metadata overrides for a group
- **GroupMetaRights** — rights metadata overrides for a group

## Authorization Model

Permission-based (no roles table). Each Account links to Rights (permissions) through AccountRightsLink.
Permission codes follow pattern: `module.action` (e.g., `uploads.create`, `groups.no_product.catalog_fast`).

## Database Patterns

- **Soft deletes**: `ArchiveLevel` (0 = active, >0 = archived) + `ArchiveTime`
- **Audit timestamps**: `CreateTime` (required), `ChangeTime` (nullable)
- **Business state**: `BusinessStatus` integer field
- **Multi-currency**: `ExchangeCurrency` + `ExchangeRate` on SuspenseLine

## Dual Group-Suspense Linking

SuspenseLine has both:
1. `GroupId` (direct FK) — **current active** group membership, nulled on ungroup
2. `SuspenseGroupLink` (junction table) — **historical** record, preserved on ungroup for audit trail

## Current Implementation Status

**Done:**
- All domain entities with EF Core configurations
- DbContext with all DbSets, 3 migrations applied (initial, nullable CatalogProductId, refresh tokens)
- Excel parsing service (ClosedXML, flexible column mapping)
- Validation service (product search + rights search + status assignment)
- Upload controller (`POST /api/upload`)
- **Dynamic grouping** — preview (`GET /api/grouping/preview`) and commit (`POST /api/grouping/commit`)
  - Raw SQL with parameterized GROUP BY, whitelist columns, filtering, sorting, pagination
  - Status 0: columns from SuspenseLine; Status 1: product columns from CatalogProduct (JOIN)
- **Saved group viewing** — `GET /api/group/no-product`, `no-rights`, `saved`, `{id}`, `{id}/suspenses`
- **Group processing** — metadata, quick catalog, link product, possible products, postpone, ungroup, back office, export
- JWT authentication (login, refresh, revoke with token rotation)
- Global exception handling middleware (`ExceptionHandlingMiddleware`)
- Standardized API responses (`ApiResponse<T>`)
- Pagination/filtering/sorting infrastructure (`PagedRequest`, `QueryableExtensions`)
- Database seeder (territories, companies, product types, sample products, rights, admin account, permissions)
- Swagger/OpenAPI with XML documentation and JWT support
- Docker support

**Not yet implemented:**
- Back office complete/return/archive operations
- User management CRUD endpoints
- Audit logging (detailed change log)
- Authorization middleware (PermissionAttribute exists but not fully wired)
- Full analytics service and endpoints

## Important Business Rules

1. **Never mix statuses** in a group — all suspenses must be either "no product" or "no rights"
2. **One group = one product** — a group can link to at most one catalog product
3. **Metadata overrides at read time** — never overwrites original suspense data
4. **Status 1/16: product data from catalog** — for grouping, display, and export, use CatalogProduct fields, not suspense fields
5. **Ungroup preserves ProductId** — when ungrouping status 16→1, the product link stays
6. **Soft delete everywhere** — data is never physically deleted
7. **All changes logged** — every status change, metadata edit, grouping action goes to audit log

## API Endpoints Overview

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/upload` | Upload Excel report, validate, save suspenses |
| GET | `/api/grouping/preview` | Dynamic grouping preview (GROUP BY + COUNT) |
| POST | `/api/grouping/commit` | Commit (fix) a dynamic group |
| GET | `/api/group/no-product` | Saved groups — status 15 |
| GET | `/api/group/no-rights` | Saved groups — status 16 |
| GET | `/api/group/saved` | All saved groups (15 + 16) |
| GET | `/api/group/{id}` | Single group by ID |
| GET | `/api/group/{id}/suspenses` | Suspenses of a group |
| GET | `/api/groups/{id}/metadata` | Group metadata |
| PUT | `/api/groups/{id}/metadata` | Update group metadata |
| GET | `/api/groups/{id}/meta-rights` | Group meta-rights |
| PUT | `/api/groups/{id}/meta-rights` | Update group meta-rights |
| POST | `/api/groups/{id}/catalog-fast` | Quick cataloging |
| GET | `/api/groups/{id}/possible-products` | Fuzzy product search |
| POST | `/api/groups/{id}/link-product` | Link group to product |
| POST | `/api/groups/{id}/send-to-backoffice` | Send to back office |
| POST | `/api/groups/{id}/postpone` | Postpone group |
| POST | `/api/groups/{id}/ungroup` | Ungroup (archive) |
| GET | `/api/groups/{id}/export-suspenses` | Export group to Excel |
| GET | `/api/groups/export` | Export groups by status |
| GET | `/api/postponed` | Postponed groups |
| POST | `/api/postponed/{id}/return` | Return from postponed |
| POST | `/api/auth/login` | JWT login |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/revoke` | Revoke refresh token |

## Known Design Notes

- **`GroupMetadata.CatalogProductId` is nullable** (`int?`) — metadata can exist before product linkage. Setting this field triggers the group's transition from "no product" to "no rights" (migration applied).
- `SuspenseLine` stores company names as strings (`SenderCompany`, `RecipientCompany`) AND has FK references (`SenderCompanyId`, `RecipientCompanyId`) — the string fields preserve original report data, FKs link to normalized company records.
- **Dynamic grouping uses raw SQL** (`GroupingSqlBuilder`) with parameterized queries and column whitelist for safety. EF Core doesn't support dynamic GROUP BY column sets.
