# CoffeeNChill

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Azure Functions](https://img.shields.io/badge/Azure_Functions-Isolated_Worker-0062AD?logo=azurefunctions&logoColor=white)
![Azure Table Storage](https://img.shields.io/badge/Azure-Table_Storage-0089D6?logo=microsoftazure&logoColor=white)
![Azure Blob Storage](https://img.shields.io/badge/Azure-Blob_Storage-0089D6?logo=microsoftazure&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?logo=docker&logoColor=white)
![Postman](https://img.shields.io/badge/Tested_with-Postman-FF6C37?logo=postman&logoColor=white)


CoffeeNChill is a cloud-enabled microservices system built for a canteen of the same name, to help manage long queues, food and beverages, menus, orders, recipe documents, and promotional photos. This project operates using Azure Storage, Serverless Functions, Docker Containerization, Web APIs, JWT Authentication, and VPS Hosting.

## Part 1: Azure Functions, Tables, Blob Storage and Docker Hub

Part 1 focuses on laying out the cloud foundation of the system, replacing the canteen's paper menus and filing cabinet documents using Azure Table Storage, Azure Blob Storage, and HTTP-triggered Azure Functions. Staff documents originally targeted Azure File Share; this was changed to Azure Blob Storage per the official module addendum, since Azurite does not emulate File storage.

## Part 2: Asynchronous Order Queuing & Docker Compose

Part 2 adds order placement and tracking on top of the Part 1 foundation. Orders are submitted over HTTP, pushed onto an Azure Storage Queue (`order-processing-queue`, base64-encoded), and processed asynchronously by a queue-triggered Azure Function that writes each order to a new `Orders` table. A Timer-triggered function then simulates the order's lifecycle (**Received → Preparing → Ready → Collected**) every 30 seconds, so status can be tracked without anyone updating it by hand. The whole system - the Functions app and Azurite - is orchestrated with Docker Compose, pulling the Functions image straight from Docker Hub (`v2.0`) with no local build step.

**Order flow:**

```
POST /api/orders/queue ──► order-processing-queue ──► ProcessOrderQueue ──► Orders table (Status = Received)
                                (base64 JSON)          (QueueTrigger)              │
                                                                                   ▼
GET /api/orders/{orderId} ◄────────────────────────── StatusSimulationTimer (every 30s: Preparing → Ready → Collected)
```

## Architecture Overview

This project follows a single-project, folder-layered architecture for the Azure Functions app (isolated worker model, .NET 8):

- **Functions/** - HTTP-, Queue- and Timer-triggered functions (MenuFunctions, MenuQueryFunctions, DocumentFunctions, OrderFunctions, OrderQueueFunctions, OrderStatusSimulationFunctions, OrderQueryFunctions)
- **Repository/** - abstracts storage access behind interfaces (IMenuRepository, IDocumentRepository, IQueueRepository, IOrderRepository), so functions never call the Azure SDKs directly
- **Models/** - domain models, Table Storage entities, and request/response DTOs (e.g. `MenuItem` vs `MenuItemEntity`, `Order` vs `OrderEntity`), kept separate so the API contract doesn't leak storage details
- **Validation/** - centralizes input validation (ModelValidator, FileValidator, OrderValidator)
- **Exceptions/** - custom exceptions mapped to proper HTTP status codes (404, 409, etc.)
- **Helpers/** - shared HTTP response formatting and stream utilities

| Store | Name | PartitionKey | RowKey |
|---|---|---|---|
| Table | `MenuItems` | Category | SKU |
| Table | `Orders` | OrderDate (yyyy-MM-dd) | OrderId |
| Blob container | `staff-docs` | - | - |
| Queue | `order-processing-queue` | - | - |

Staff documents are streamed directly to and from Blob Storage on upload/download (never buffered fully in memory), with PDF-only validation on extension, MIME type, `%PDF-` file signature, and a 10 MB size cap enforced while streaming.

## Tech Requirements

| Tool | Version | Link |
|---|---|---|
| .NET SDK | 8.0 | [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) |
| Azure Functions Core Tools | v4 | [Install guide - Microsoft Learn](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local) |
| Docker Desktop | latest | [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) |
| Postman | latest | [postman.com/downloads](https://www.postman.com/downloads/) |
| Visual Studio 2022 | latest | [visualstudio.microsoft.com](https://visualstudio.microsoft.com/) (or VS Code with the C# Dev Kit extension) |

## Quick Start (Part 2 - Docker Compose, recommended)

The only thing needed is Docker Desktop. No source code or local build is required - the Functions image is pulled from Docker Hub.

1. From the repository root, run:
   ```
   docker compose up
   ```
2. Docker pulls `mcr.microsoft.com/azure-storage/azurite` and `st10477718/coffeenchill-functions:v2.0`, then starts both containers on a shared network. The Functions container reaches Azurite by its service name (`azurite`), not `localhost`.
3. Once the Functions host reports it is running, the API is available at `http://localhost:7071/api`.
4. Import `/docs/CoffeeNChill.postman_collection.json` into Postman - its `baseUrl` is already set to `http://localhost:7071/api`.
5. To stop everything: `docker compose down`

Docker Hub Images and Versioning

Version - v1.0

Original Part 1 application image

Version - v2.0

Part 2 image containing the integrated application updates

Docker Compose uses the published v2.0 image. It does not build the Functions image from local source code when starting the application.

## Local Development Setup (Visual Studio / func start)

1. Install the tools listed above
2. Clone the repo and open `CLDV6212_Poe_Group1_CoffeeNChill.slnx`
3. Run Azurite in its own container:
   ```
   docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
   ```
4. `local.settings.json` is already included in the repo and points at Azurite - no manual configuration needed
5. Restore and run: `dotnet restore`, then `func start` (or press F5 in Visual Studio)
6. The host lists all function routes on `http://localhost:7100/api/...` - in Postman, change `baseUrl` to `http://localhost:7100/api` when running this way

## Docker Commands

**Part 2 - Docker Compose (orchestrated, pulls from Docker Hub):**
```
docker compose up        # start Azurite + Functions
docker compose ps        # check both containers are running
docker compose logs -f functions   # watch queue/timer logs live
docker compose down      # stop and remove the containers
```

**Building, tagging and publishing an image version (maintainers only, from the repository root):**
```
docker build -t coffeenchill-functions ./CLDV6212_Poe_Group1_CoffeeNChill
docker tag coffeenchill-functions st10477718/coffeenchill-functions:v2.0
docker push st10477718/coffeenchill-functions:v2.0
```

**Part 1 - Standalone containers (kept for reference):**
```
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
docker run -p 7071:80 st10477718/coffeenchill-functions:v1.0
```

Storage connection settings are baked into the image via `ENV` (per the module addendum). In Docker Compose, the `environment:` block overrides them so every connection string points at the `azurite` service instead of `host.docker.internal`.

**Published images (Docker Hub - `st10477718/coffeenchill-functions`):**
- `v1.0` - Part 1 (Menu + Documents)
- `v2.0` - Part 2 (adds Order queue, queue trigger, timer status simulation)

## Menu Endpoints

| Method | Route |
|---|---|
| POST | `/api/menu` |
| GET | `/api/menu` |
| GET | `/api/menu/category/{category}` |
| PUT | `/api/menu/{category}/{id}` |
| DELETE | `/api/menu/{category}/{id}` |

## Document Endpoints

| Method | Route |
|---|---|
| POST | `/api/documents/upload` |
| GET | `/api/documents` |
| GET | `/api/documents/download/{fileName}` |

## Order Endpoints & Triggers

| Type | Name / Route | Purpose |
|---|---|---|
| HTTP POST | `/api/orders/queue` | Validates an order and sends it to `order-processing-queue` |
| Queue Trigger | `ProcessOrderQueue` | Decodes the message and writes the order to the `Orders` table as `Received` |
| Timer Trigger | `StatusSimulationTimer` (every 30s) | Moves each active order one step: Received → Preparing → Ready → Collected |
| HTTP GET | `/api/orders/{orderId}` | Returns the order and its current status (404 if not found) |

Sample order body:
```json
{
  "OrderId": "ORD-2026-8801",
  "CustomerName": "Jane Smith",
  "SelectedItemSKUs": ["COF-001", "PAS-104"],
  "TotalPrice": 65.00,
  "OrderTimestamp": "2026-10-12T10:15:00Z"
}
```

## Menus and Orders

| Who | Meaning |
|---|---|
| Public | No token needed |
| Admin or Staff | Valid token with the `Admin` or `Staff` role. No token returns `401`; a `Customer` token returns `403` |

### Menu

| Method | Route | Who can call it | Success | Other responses |
|---|---|---|---|---|
| GET | `/api/menu` | Public | `200` full menu | |
| GET | `/api/menu/category/{category}` | Public | `200` items in the category (empty list if none) | |
| POST | `/api/menu` | Admin or Staff | `201` item created | `400` validation, `401`, `403`, `409` category and SKU already exist |
| PUT | `/api/menu/{category}/{sku}` | Admin or Staff | `200` item updated | `400` validation, `401`, `403`, `404` |
| DELETE | `/api/menu/{category}/{sku}` | Admin or Staff | `204` item deleted | `401`, `403`, `404` |

### Orders

| Method | Route | Who can call it | Success | Other responses |
|---|---|---|---|---|
| POST | `/api/orders/queue` | Public | `202` accepted onto the queue (returns the `orderId`) | `400` validation, `409` order id already queued |
| GET | `/api/orders/{orderId}` | Public | `200` the order and its current status | `404` not found (it may still be waiting in the queue) |
| PUT | `/api/orders/status` | Admin or Staff | `200` status updated | `400` unknown status, `401`, `403`, `404`, `409` order can only move forward |

Notes:
- Placing an order is asynchronous. The order appears in `GET /api/orders/{orderId}` a few seconds after the `202`, with status `Received`. If `OrderId` or `OrderTimestamp` is omitted, the Gateway generates them.
- Statuses move forward only: `Received` → `Preparing` → `Ready` → `Collected`. A background timer also advances active orders every 30 seconds, so a manual update to a status the order has already passed returns `409`.

```

## Changelog

### Part 2 - Asynchronous Order Queuing & Docker Compose
- Added `Order` / `OrderEntity` / `OrderRequest` models and `OrderValidator`
- Added `POST /api/orders/queue` - validates an order and pushes it (base64-encoded) onto `order-processing-queue`
- Added `ProcessOrderQueue` - queue-triggered function that writes each order to the `Orders` table (PartitionKey = OrderDate, RowKey = OrderId, Status = "Received")
- Added `StatusSimulationTimer` - Timer-triggered function that progresses active orders Received → Preparing → Ready → Collected
- Added `GET /api/orders/{orderId}` - returns an order's current status
- Added `docker-compose.yml` at the repository root - orchestrates Azurite and the Functions app, pulling `coffeenchill-functions:v2.0` from Docker Hub with no local build step
- Published `v2.0` to Docker Hub alongside a properly tagged `v1.0`
- Updated the Postman collection with an `Orders` folder (queue placement, status checks, and 400/404 negative cases)

### Corrections from Part 1 feedback
- **Blob Storage:** upload and download are now fully stream-based (`StreamHelper`, `MaxLengthStream`, `DownloadStreamingAsync`), with `%PDF-` signature checks, file-name sanitising, and a 409 Conflict for duplicate uploads
- **Docker Hub:** `docker tag` / `docker push` commands documented, `v1.0` and `v2.0` both published
- **Postman:** re-tested against the Docker container; `baseUrl` is a collection variable pointing at `http://localhost:7071/api`; negative test cases added for Documents and Orders
- **Menu:** category filtering (`GET /api/menu/category/{category}`, filtered server-side by PartitionKey) and the `MenuItemEntity` → `MenuItem` DTO mapping are now shown explicitly in the README and video

## Contributions

### Part 1
- **Jensen** - Project scaffold and folder structure, Menu Table Storage repository, CreateMenuItem/UpdateMenuItem/DeleteMenuItem, dependency injection wiring, integration of all team members' work
- **Julian** - GetAllMenuItems/GetMenuItemsByCategory endpoints, README
- **Maheshen** - Staff document Blob Storage repository and functions (Upload/List/Download), file validation
- **Jordan** - Docker containerization, Azurite setup, Docker Hub publishing

### Part 2
- **Jensen** - `OrderEntity`, `IOrderRepository` / `TableOrderRepository`, `ProcessOrderQueue` (queue trigger), `StatusSimulationTimer` (timer trigger), `GetOrderStatus`, integration and bug fixes across branches, README
- **Julian** - `Order` / `OrderRequest` models, `OrderValidator`, `IQueueRepository` / `QueueRepository`, QueueClient registration, `PlaceOrder` endpoint, Postman order example
- **Maheshen** - Part 1 Blob Storage correction (stream-based transfer, `StreamHelper`, PDF signature validation, file-name sanitising, 409 Conflict handling), rebuilt Postman collection with Orders folder and negative tests
- **Jordan** - `docker-compose.yml` (Azurite + Functions services, compose networking), `v1.0` tag correction, `v2.0` image build and Docker Hub publishing, end-to-end Compose verification

## Postman Collection

The full collection lives in `/docs/CoffeeNChill.postman_collection.json`, organized into **Menu**, **Documents** and **Orders** folders, using `baseUrl` and `orderId` collection variables. Each folder includes negative cases (400 / 404 / 409) alongside the happy paths. `baseUrl` defaults to `http://localhost:7071/api` (Docker Compose); switch it to `http://localhost:7100/api` when running locally from Visual Studio.

## Youtube Link

### Part 1 : https://youtu.be/ooQP1ORtmnc
### Part 2 : <!-- paste the unlisted Part 2 YouTube link here -->
