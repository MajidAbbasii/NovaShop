[![npm (scoped with tag)](https://img.shields.io/npm/v/antd-jalali/latest.svg?style=flat-square)](https://npmjs.com/package/antd-jalali)
[![npm](https://img.shields.io/npm/dt/antd-jalali.svg?style=flat-square)](https://npmjs.com/package/antd-jalali)

# Ant-Design Jalali DatePicker

A wrapper for ant-design date picker and calendar to support Jalali calendar type with [Day.js](https://github.com/iamkun/dayjs) and [jalaliday](https://github.com/alibaba-aero/jalaliday)

## Demo

[https://saeedrahimi.github.io/antd-jalali/](https://saeedrahimi.github.io/antd-jalali/)

## Top Features

- Support Ant Design Version 4 and 5
- Support React 16/17/18
- Fix All RTL Issues

## Installation

### Ant version 5.x.x

```
npm i antd-jalali@v2.x.x
```

### Ant version 4.x.x

```
npm i antd-jalali@v1.4.x
```

## Usage

```ts
import React from "react";
import ReactDOM from "react-dom";
import { DatePicker, ConfigProvider } from "antd";
import { DatePicker as DatePickerJalali, Calendar, JalaliLocaleListener } from "antd-jalali";
import fa_IR from "antd/lib/locale/fa_IR";
import en_US from "antd/lib/locale/en_US";
import "antd/dist/antd.css";
import "./index.css";

ReactDOM.render(
  <div className="App">
    Gregorian: <DatePicker />
    <br />
    <br />
    <ConfigProvider locale={fa_IR} direction="rtl">
      <JalaliLocaleListener />
      Jalali: <DatePickerJalali />
      Jalali RangePicker: <DatePickerJalali.RangePicker />
      <br />
      <br />
      <Calendar />
    </ConfigProvider>
  </div>,
  document.getElementById("root")
);
```

### How to set value

You should pass dayjs object with [jalali calendar](https://github.com/alibaba-aero/jalaliday)

```jsx
import dayjs from 'dayjs'
import { DatePicker as DatePickerJalali, Calendar as CalendarJalali, useJalaliLocaleListener } from "antd-jalali";

// You should call this hook in child component of <ConfigProvider>
// You can also use component helper for this hook <JalaliLocaleListener>
useJalaliLocaleListener();

// If you want to all new instanses of dayjs use jalali calendar (no matter what is the locale),
// you can set default calendar for dayjs and remove useJalaliLocaleListener hook.
dayjs.calendar('jalali');

const date = dayjs("1403-01-01", {jalali:true});

<DatePickerJalali defaultValue={date}/>
<CalendarJalali  value={date}/>
```

also you can create a jalali date without changing default calendar

```js
const date = dayjs();
const jalaliDate = date.calendar("jalali");
```

You can read more information about daysjs jalali on [jalaliday repo](https://github.com/alibaba-aero/jalaliday).

## Contributors

<a href="https://github.com/saeedrahimi">
<img src="https://github.com/saeedrahimi.png" width="60px;"/></a>
<a href="https://github.com/masoudit">
<img src="https://github.com/masoudit.png" width="60px;"/></a>
<a href="https://github.com/hamidrezaghanbari">
<img src="https://github.com/hamidrezaghanbari.png" width="60px;"/></a>
<a href="https://github.com/mohas">
<img src="https://github.com/mohas.png" width="60px;"/></a>

---

# NovaShop (Backend)

This repository also contains the NovaShop backend sample located under backend/src.

Getting started (NovaShop API)

1. Requirements
   - .NET 10 SDK
   - SQL Server or LocalDB
   - (Optional) Redis for distributed cache
   - (Optional) RabbitMQ for messaging

2. Configuration
   - The API reads configuration from backend/src/NovaShop.Api/appsettings.json and appsettings.Development.json.
   - JWT settings are under the `Jwt` section. Do NOT store production secrets in appsettings.json.

Set a development secret with user-secrets (recommended):

   cd backend/src/NovaShop.Api
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "<your-secret-here>"

Or set environment variable (Windows PowerShell):

   $env:Jwt__Key = "<your-secret-here>"

3. Cache provider
   - Default is Memory cache. To use Redis, set `Cache:Provider` to `Redis` and set `Cache:RedisConnectionString` accordingly.
   - Example (appsettings.Development.json):

     "Cache": { "Provider": "Redis", "RedisConnectionString": "localhost:6379" }

4. Database
   - Connection string is in `backend/src/NovaShop.Api/appsettings.json` under `ConnectionStrings:DefaultConnection`.
   - The API runs `context.Database.Migrate()` at startup to apply migrations.

5. Run
   - From solution root:
       dotnet build
       dotnet run --project backend/src/NovaShop.Api

6. Notes
   - JWT key should be long and stored securely (user-secrets, env vars, or Key Vault in production).
   - CORS is configured to allow all origins for development; tighten it for production.
   - Hangfire dashboard is available at /hangfire and uses the configured SQL Server storage.
   - RabbitMQ is expected at localhost with default guest/guest credentials for local development.

7. Observability & Resilience (added)
   - Serilog is configured for structured logging (console + file). Configure sinks via appsettings if needed.
   - Health checks available at /health (SQL Server + Redis when configured).
   - Basic OpenTelemetry tracing is registered (AspNetCore and SqlClient instrumentation). Configure exporters as needed.
   - HttpClient uses Polly retry policies for transient faults.

