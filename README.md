# CraftConnect

CraftConnect is an Angular client with an ASP.NET Core and SQLite API for customer service requests, craftsman approvals, bookings, and the accepted-date conflict demo.

## Clean setup

1. Install .NET 10 SDK, Node.js, and npm.
2. From the repository root, restore the API:

   ```powershell
   dotnet restore api/api.csproj
   ```

   Restore the repository's local EF command-line tool as well:

   ```powershell
   cd api
   dotnet tool restore
   cd ..
   ```

3. Restore the Angular dependencies:

   ```powershell
   cd client
   npm ci
   cd ..
   ```

4. Apply the database migrations:

   ```powershell
   dotnet ef database update --project api/api.csproj --startup-project api/api.csproj
   ```

   The local SQLite database is `api/crafts.db`.

5. Create the demo data from a clean database. The command is idempotent and creates one admin, three approved craftsmen across three crafts, two customers, and two pending requests for the same craftsman on the same date:

   ```powershell
   cd api
   dotnet run -- --seed
   cd ..
   ```

   Seed credentials use the password `Seed123!`. The admin account is `admin@craftconnect.local` with password `Admin123!`.

6. Start the API on port `5128`:

   ```powershell
   dotnet run --project api/api.csproj --urls http://localhost:5128
   ```

7. In a second terminal, start Angular on port `4200`:

   ```powershell
   cd client
   npm start -- --port 4200
   ```

8. Open `http://localhost:4200`.

The Angular services call the API at `http://localhost:5128`. The two applications use separate ports because one TCP port cannot host both development servers.

## Verification

Run the Angular production build:

```powershell
cd client
npm run build
```

Run the five SQLite integration tests:

```powershell
dotnet test api.Tests/api.Tests.csproj
```

The list endpoints accept `page` and `pageSize`; `pageSize` is capped at 20. Existing clients may omit both parameters and receive the first page.
