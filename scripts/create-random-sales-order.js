#!/usr/bin/env node

const fs = require("fs");
const http = require("http");
const https = require("https");
const path = require("path");

const DEFAULT_ITEMS = ["BOX1", "BOX2", "BOX3", "BOX4", "BOX5", "BOX6"];

function parseArgs(argv) {
  const args = {
    config: path.resolve(__dirname, "../Service/config/Configurations.yaml"),
    cardCode: undefined,
    items: DEFAULT_ITEMS,
    warehouse: "BIN",
    unitPrice: 1,
    minBoxes: 1,
    maxBoxes: 10,
    minDozens: 0,
    maxDozens: 3,
    comments: "Random sales order created by create-random-sales-order.js",
    dryRun: false,
    validateOnly: false,
  };

  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    const next = () => {
      if (i + 1 >= argv.length) {
        throw new Error(`Missing value for ${arg}`);
      }
      i += 1;
      return argv[i];
    };

    switch (arg) {
      case "--config":
        args.config = path.resolve(next());
        break;
      case "--card-code":
        args.cardCode = next();
        break;
      case "--items":
        args.items = next().split(",").map((item) => item.trim()).filter(Boolean);
        break;
      case "--warehouse":
        args.warehouse = next();
        break;
      case "--unit-price":
        args.unitPrice = Number(next());
        break;
      case "--min-boxes":
        args.minBoxes = Number(next());
        break;
      case "--max-boxes":
        args.maxBoxes = Number(next());
        break;
      case "--min-dozens":
        args.minDozens = Number(next());
        break;
      case "--max-dozens":
        args.maxDozens = Number(next());
        break;
      case "--comments":
        args.comments = next();
        break;
      case "--dry-run":
        args.dryRun = true;
        break;
      case "--validate-only":
        args.validateOnly = true;
        break;
      case "--help":
      case "-h":
        printHelp();
        process.exit(0);
        break;
      default:
        throw new Error(`Unknown argument: ${arg}`);
    }
  }

  if (args.items.length === 0) {
    throw new Error("At least one item is required");
  }
  if (!Number.isFinite(args.unitPrice) || args.unitPrice < 0) {
    throw new Error("--unit-price must be zero or greater");
  }
  for (const [name, value] of [
    ["--min-boxes", args.minBoxes],
    ["--max-boxes", args.maxBoxes],
    ["--min-dozens", args.minDozens],
    ["--max-dozens", args.maxDozens],
  ]) {
    if (!Number.isInteger(value) || value < 0) {
      throw new Error(`${name} must be a non-negative integer`);
    }
  }
  if (args.minBoxes > args.maxBoxes) {
    throw new Error("--min-boxes cannot be greater than --max-boxes");
  }
  if (args.minDozens > args.maxDozens) {
    throw new Error("--min-dozens cannot be greater than --max-dozens");
  }

  return args;
}

function printHelp() {
  console.log(`Usage:
  node scripts/create-random-sales-order.js [options]

Creates a SAP B1 Sales Order through Service Layer Orders.

Defaults:
  Customer: random valid customer BP
  Items: BOX1,BOX2,BOX3,BOX4,BOX5,BOX6
  Warehouse: BIN
  Scenario per item: 1-10 boxes plus 0-3 dozens
  Unit price: 1

Options:
  --config <path>       Configuration.yaml path
  --card-code <code>    Use a specific customer BP instead of a random one
  --items <csv>         Comma-separated item codes
  --warehouse <code>    Warehouse code for all lines
  --unit-price <number> Unit price per line
  --min-boxes <number>  Minimum boxes per item
  --max-boxes <number>  Maximum boxes per item
  --min-dozens <number> Minimum dozens per item
  --max-dozens <number> Maximum dozens per item
  --comments <text>     Document comments
  --dry-run             Print payload without posting to SAP
  --validate-only       Check BP, warehouse, and items without creating
`);
}

function parseYamlScalar(value) {
  const trimmed = value.trim();
  if (trimmed === "null" || trimmed === "~") {
    return null;
  }
  if ((trimmed.startsWith("\"") && trimmed.endsWith("\"")) ||
      (trimmed.startsWith("'") && trimmed.endsWith("'"))) {
    return trimmed.slice(1, -1);
  }
  return trimmed;
}

function readIndentedBlock(lines, blockName) {
  const start = lines.findIndex((line) => line.trim() === `${blockName}:`);
  if (start < 0) {
    return {};
  }

  const result = {};
  for (let i = start + 1; i < lines.length; i += 1) {
    const line = lines[i];
    if (/^\S/.test(line) && line.trim().endsWith(":")) {
      break;
    }
    if (!line.startsWith("  ") || line.trim().startsWith("#") || line.trim() === "") {
      continue;
    }

    const match = line.match(/^\s{2}([^:#]+):\s*(.*)$/);
    if (!match) {
      continue;
    }

    const key = match[1].trim();
    const rawValue = match[2].trim();
    if (rawValue === "") {
      result[key] = {};
      continue;
    }
    result[key] = parseYamlScalar(rawValue);
  }
  return result;
}

function loadSettings(configPath) {
  const text = fs.readFileSync(configPath, "utf8");
  const lines = text.split(/\r?\n/);
  const sbo = readIndentedBlock(lines, "SboSettings");

  const required = ["ServiceLayerUrl", "Database", "User", "Password"];
  for (const key of required) {
    if (!sbo[key]) {
      throw new Error(`Missing SboSettings.${key} in ${configPath}`);
    }
  }

  return {
    serviceLayerUrl: String(sbo.ServiceLayerUrl).replace(/\/+$/, ""),
    companyDb: String(sbo.Database),
    userName: String(sbo.User),
    password: String(sbo.Password),
  };
}

function today() {
  return new Date().toISOString().slice(0, 10);
}

function randInt(min, max) {
  return min + Math.floor(Math.random() * (max - min + 1));
}

function quoteODataKey(value) {
  return String(value).replace(/'/g, "''");
}

function requestJson(method, targetUrl, body, headers = {}) {
  return new Promise((resolve, reject) => {
    const parsed = new URL(targetUrl);
    const isHttps = parsed.protocol === "https:";
    const payload = body == null ? undefined : JSON.stringify(body);
    const client = isHttps ? https : http;

    const request = client.request({
      method,
      hostname: parsed.hostname,
      port: parsed.port || (isHttps ? 443 : 80),
      path: `${parsed.pathname}${parsed.search}`,
      rejectUnauthorized: false,
      headers: {
        Accept: "application/json",
        ...(payload ? {
          "Content-Type": "application/json",
          "Content-Length": Buffer.byteLength(payload),
        } : {}),
        ...headers,
      },
    }, (response) => {
      const chunks = [];
      response.on("data", (chunk) => chunks.push(chunk));
      response.on("end", () => {
        const raw = Buffer.concat(chunks).toString("utf8");
        let data = null;
        if (raw.trim() !== "") {
          try {
            data = JSON.parse(raw);
          } catch {
            data = raw;
          }
        }
        resolve({
          ok: response.statusCode >= 200 && response.statusCode < 300,
          statusCode: response.statusCode,
          statusMessage: response.statusMessage,
          headers: response.headers,
          data,
          raw,
        });
      });
    });

    request.on("error", reject);
    if (payload) {
      request.write(payload);
    }
    request.end();
  });
}

function getErrorMessage(response) {
  const data = response.data;
  if (data && typeof data === "object") {
    const message = data.error?.message;
    if (typeof message === "string") {
      return message;
    }
    if (message && typeof message === "object") {
      return message.value || message.text || JSON.stringify(message);
    }
    if (Array.isArray(data.error?.details) && data.error.details[0]?.message) {
      return data.error.details[0].message;
    }
  }
  return response.raw || `HTTP ${response.statusCode}: ${response.statusMessage}`;
}

function buildCookie(loginResponse) {
  const setCookie = loginResponse.headers["set-cookie"];
  if (Array.isArray(setCookie) && setCookie.length > 0) {
    return setCookie.map((cookie) => cookie.split(";")[0]).join("; ");
  }

  const sessionId = loginResponse.data?.SessionId;
  if (!sessionId) {
    throw new Error("Login succeeded but no Service Layer session cookie or SessionId was returned");
  }
  return `B1SESSION=${sessionId}`;
}

async function login(settings) {
  const response = await requestJson("POST", `${settings.serviceLayerUrl}/b1s/v2/Login`, {
    CompanyDB: settings.companyDb,
    UserName: settings.userName,
    Password: settings.password,
  });

  if (!response.ok) {
    throw new Error(`Service Layer login failed: ${getErrorMessage(response)}`);
  }

  return buildCookie(response);
}

async function getJson(settings, cookie, endpoint) {
  const response = await requestJson(
    "GET",
    `${settings.serviceLayerUrl}/b1s/v2/${endpoint}`,
    null,
    { Cookie: cookie },
  );

  if (!response.ok) {
    throw new Error(getErrorMessage(response));
  }

  return response.data;
}

async function getRandomCustomer(settings, cookie) {
  const data = await getJson(
    settings,
    cookie,
    "BusinessPartners?$select=CardCode,CardName&$filter=CardType eq 'cCustomer' and Valid eq 'tYES'&$top=100",
  );
  const customers = Array.isArray(data.value) ? data.value : [];
  if (customers.length === 0) {
    throw new Error("No valid customer BP was returned by Service Layer");
  }
  return customers[randInt(0, customers.length - 1)];
}

async function loadItem(settings, cookie, itemCode) {
  return await getJson(
    settings,
    cookie,
    `Items('${quoteODataKey(itemCode)}')?$select=ItemCode,ItemName,SalesItemsPerUnit,SalesQtyPerPackUnit,PurchaseItemsPerUnit,PurchaseQtyPerPackUnit`,
  );
}

async function validateReference(settings, cookie, endpoint, label) {
  try {
    await getJson(settings, cookie, endpoint);
    return { label, ok: true };
  } catch (error) {
    return { label, ok: false, message: error.message };
  }
}

async function validateReferences(settings, cookie, args, cardCode) {
  const checks = [];
  checks.push(await validateReference(settings, cookie, `BusinessPartners('${quoteODataKey(cardCode)}')`, `BP ${cardCode}`));
  checks.push(await validateReference(settings, cookie, `Warehouses('${quoteODataKey(args.warehouse)}')`, `Warehouse ${args.warehouse}`));
  for (const item of args.items) {
    checks.push(await validateReference(settings, cookie, `Items('${quoteODataKey(item)}')`, `Item ${item}`));
  }

  console.log("\nReference check:");
  for (const check of checks) {
    if (check.ok) {
      console.log(`  OK   ${check.label}`);
    } else {
      console.log(`  FAIL ${check.label}: ${check.message}`);
    }
  }

  return checks.every((check) => check.ok);
}

function getDozenSize(item) {
  return Number(item.SalesItemsPerUnit || item.PurchaseItemsPerUnit || 12) || 12;
}

function getBoxSize(item) {
  const dozenSize = getDozenSize(item);
  const packsPerBox = Number(item.SalesQtyPerPackUnit || item.PurchaseQtyPerPackUnit || 1) || 1;
  return dozenSize * packsPerBox;
}

function buildScenario(args, item) {
  const boxes = randInt(args.minBoxes, args.maxBoxes);
  const dozens = randInt(args.minDozens, args.maxDozens);
  const dozenSize = getDozenSize(item);
  const boxSize = getBoxSize(item);
  const quantity = (boxes * boxSize) + (dozens * dozenSize);

  return {
    itemCode: item.ItemCode,
    itemName: item.ItemName,
    boxes,
    dozens,
    dozenSize,
    boxSize,
    quantity,
  };
}

function buildPayload(args, cardCode, scenarios) {
  const date = today();
  return {
    CardCode: cardCode,
    DocDate: date,
    DocDueDate: date,
    TaxDate: date,
    Comments: args.comments,
    DocumentLines: scenarios.map((scenario) => ({
      ItemCode: scenario.itemCode,
      Quantity: scenario.quantity,
      WarehouseCode: args.warehouse,
      UnitPrice: args.unitPrice,
      UseBaseUnits: "tYES",
      FreeText: `${scenario.boxes} boxes + ${scenario.dozens} dozens`,
    })),
  };
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const settings = loadSettings(args.config);
  const cookie = await login(settings);
  const customer = args.cardCode
    ? { CardCode: args.cardCode, CardName: "(specified)" }
    : await getRandomCustomer(settings, cookie);

  const referencesOk = await validateReferences(settings, cookie, args, customer.CardCode);
  if (args.validateOnly) {
    if (!referencesOk) {
      process.exitCode = 1;
    }
    return;
  }
  if (!referencesOk) {
    throw new Error("Reference validation failed; not creating the sales order");
  }

  const itemData = [];
  for (const itemCode of args.items) {
    itemData.push(await loadItem(settings, cookie, itemCode));
  }

  const scenarios = itemData.map((item) => buildScenario(args, item));
  const payload = buildPayload(args, customer.CardCode, scenarios);

  console.log("Random Sales Order request");
  console.log(`  Config: ${args.config}`);
  console.log(`  Service Layer: ${settings.serviceLayerUrl}`);
  console.log(`  Company DB: ${settings.companyDb}`);
  console.log(`  User: ${settings.userName}`);
  console.log(`  Password: ${"*".repeat(settings.password.length)}`);
  console.log(`  Customer: ${customer.CardCode} ${customer.CardName ? `(${customer.CardName})` : ""}`);
  console.log(`  Warehouse: ${args.warehouse}`);
  console.log("");

  console.log("Scenario:");
  for (const scenario of scenarios) {
    console.log(`  ${scenario.itemCode}: ${scenario.boxes} boxes + ${scenario.dozens} dozens = ${scenario.quantity} base units`);
  }

  console.log("");
  console.log(JSON.stringify(payload, null, 2));

  if (args.dryRun) {
    console.log("\nDry run only. No sales order was created.");
    return;
  }

  const create = await requestJson(
    "POST",
    `${settings.serviceLayerUrl}/b1s/v2/Orders`,
    payload,
    { Cookie: cookie },
  );

  if (!create.ok) {
    throw new Error(`Sales order creation failed: ${getErrorMessage(create)}`);
  }

  console.log("\nCreated Sales Order");
  console.log(`  DocEntry: ${create.data?.DocEntry ?? "N/A"}`);
  console.log(`  DocNum: ${create.data?.DocNum ?? "N/A"}`);
}

main().catch((error) => {
  console.error(`Error: ${error.message}`);
  process.exitCode = 1;
});
