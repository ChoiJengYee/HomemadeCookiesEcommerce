# Single-vendor Homemade Cookie E-commerce (Prototype)

ASP.NET Core Web API backend, vanilla HTML/CSS/JS frontend, and PostgreSQL database. The prototype demonstrates **Factory Method**, **Facade**, **State**, and **Singleton** (database connection) per the system design in `.cursor/rules/cookie-system.mdc`.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (project targets **net10.0**)
- [PostgreSQL](https://www.postgresql.org/) 15+ and [pgAdmin](https://www.pgadmin.org/)
- Web browser

## Database setup (pgAdmin)

1. Create database: `homemade_cookie_db`
2. Run scripts in order:
   - [`database/schema.sql`](database/schema.sql)
   - [`database/seed.sql`](database/seed.sql)
3. Update connection string in [`backend/HomemadeCookie.Api/appsettings.json`](backend/HomemadeCookie.Api/appsettings.json) or `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=homemade_cookie_db;Username=postgres;Password=YOUR_PASSWORD"
}
```

## Run the application

```bash
cd backend/HomemadeCookie.Api
dotnet run
```

- API + static frontend: **http://localhost:5017**
- Health check: **http://localhost:5017/api/health**

Open the shop at `http://localhost:5017/index.html`.

### Demo accounts (seed data)

| Role     | Email                         | Password     | user_id |
|----------|-------------------------------|--------------|---------|
| Customer | customer@homemadecookies.com  | customer123  | 2       |
| Admin    | admin@homemadecookies.com     | admin123     | 1       |

The storefront uses **customer id 2** by default (see `frontend/js/api.js`).

## API endpoints (11)

| Method | Route | Pattern |
|--------|-------|---------|
| GET | `/api/health` | Singleton (DB via single connection entry point) |
| GET | `/api/products` | — |
| GET | `/api/cart/{customerId}` | — |
| POST | `/api/cart/{customerId}/items` | — |
| PUT | `/api/cart/{customerId}/items/{cookieId}` | — |
| DELETE | `/api/cart/{customerId}/items/{cookieId}` | — |
| POST | `/api/orders/checkout` | **Facade** |
| GET | `/api/orders/{id}/status` | **State** |
| POST | `/api/orders/{id}/cancel` | **State** |
| POST | `/api/admin/cookies` | **Factory** |
| GET | `/api/admin/orders` | — |
| PUT | `/api/admin/orders/{id}/advance` | **State** |

## Design pattern traceability

| Pattern | Where to look |
|---------|----------------|
| **Singleton** | [`backend/HomemadeCookie.Api/Infrastructure/DatabaseConnection.cs`](backend/HomemadeCookie.Api/Infrastructure/DatabaseConnection.cs) — all repositories use `DatabaseConnection.Instance.CreateConnection()` |
| **Factory Method** | [`backend/HomemadeCookie.Api/Patterns/Factory/`](backend/HomemadeCookie.Api/Patterns/Factory/) + `POST /api/admin/cookies` + [`frontend/admin/products.html`](frontend/admin/products.html) |
| **Facade** | [`backend/HomemadeCookie.Api/Patterns/Facade/OrderManagementFacade.cs`](backend/HomemadeCookie.Api/Patterns/Facade/OrderManagementFacade.cs) + `POST /api/orders/checkout` + [`frontend/checkout.html`](frontend/checkout.html) |
| **State** | [`backend/HomemadeCookie.Api/Patterns/State/`](backend/HomemadeCookie.Api/Patterns/State/) + advance/cancel/status APIs + [`frontend/admin/orders.html`](frontend/admin/orders.html), [`frontend/track-order.html`](frontend/track-order.html) |

## End-to-end demo script

1. **Factory:** Admin → Add cookie → create e.g. Strawberry via FruitFactory → appears on Shop.
2. **Facade (success):** Shop → add to cart → Checkout → card `1234-5678` → order placed, cart cleared.
3. **Facade (out of stock):** Set a cookie stock to 0 in DB → checkout shows out-of-stock message.
4. **Facade (pending):** Checkout with card containing `PENDING` → pending order message.
5. **State (advance):** Admin → Orders → Advance through Pending → Confirmed → Baking → Ready → Completed.
6. **State (cancel):** Place order → Track order → Cancel while Pending.
7. **State (forbidden):** Try cancel on Confirmed, or advance on Completed → error message.

## Mock payment (Facade)

| Card input contains | Result |
|---------------------|--------|
| (normal digits) | Success — stock reduced, order saved, confirmation logged |
| `PENDING` or `LIMIT` | Payment pending — order saved, pending email logged |
| `FAIL` | Payment declined — no order |

## Project structure

```
Demo_Homemade Cookie/
├── database/          # schema.sql, seed.sql
├── backend/           # HomemadeCookie.Api (C#)
├── frontend/          # HTML, CSS, JS
└── README.md
```
