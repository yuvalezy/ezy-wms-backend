#!/usr/bin/env node

const fs = require("fs");
const http = require("http");
const https = require("https");
const path = require("path");

const DEFAULT_ITEMS = ["BOX1", "BOX2", "BOX3", "BOX4", "BOX5", "BOX6"];

function parseArgs(argv) {
  const args = {
    config: path.resolve(__dirname, "../Service/config/Configurations.yaml"),
    cardCode: "V20000",
    items: DEFAULT_ITEMS,
    quantity: 1480,
    unitPrice: 1,
    warehouse: undefined,
    comments: "Manual Goods Receipt PO created by create-grpo.js",
    dryRun: false,
    spreadBins: false,
    binCount: 6,
    binEntries: undefined,
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
      case "--quantity":
        args.quantity = Number(next());
        break;
      case "--unit-price":
        args.unitPrice = Number(next());
        break;
      case "--warehouse":
        args.warehouse = next();
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
      case "--spread-bins":
        args.spreadBins = true;
        break;
      case "--bin-count":
        args.binCount = Number(next());
        break;
      case "--bin-entries":
        args.binEntries = next().split(",").map((entry) => Number(entry.trim())).filter(Number.isFinite);
        args.spreadBins = true;
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
  if (!Number.isFinite(args.quantity) || args.quantity <= 0) {
    throw new Error("--quantity must be a positive number");
  }
  if (!Number.isFinite(args.unitPrice) || args.unitPrice < 0) {
    throw new Error("--unit-price must be zero or greater");
  }
  if (!Number.isInteger(args.binCount) || args.binCount <= 1) {
    throw new Error("--bin-count must be an integer greater than 1");
  }
  if (args.binEntries && args.binEntries.length <= 1) {
    throw new Error("--bin-entries must include at least two bin entries");
  }

  return args;
}

function printHelp() {
  console.log(`Usage:
  node scripts/create-grpo.js [options]

Creates a SAP B1 Goods Receipt PO through Service Layer PurchaseDeliveryNotes.

Defaults:
  BP: V20000
  Items: BOX1,BOX2,BOX3,BOX4,BOX5,BOX6
  Quantity: 1480 each
  Unit price: 1 each
  Config: Service/config/Configurations.yaml
  Warehouse: first warehouse key in Configurations.yaml, unless --warehouse is provided

Options:
  --config <path>       Configuration.yaml path
  --card-code <code>    Vendor BP code
  --items <csv>         Comma-separated item codes
  --quantity <number>   Quantity per line
  --unit-price <number> Unit price per line
  --warehouse <code>    Warehouse code for all lines
  --comments <text>     Document comments
  --dry-run             Print payload without posting to SAP
  --validate-only       Check BP, warehouse, items, and bins without creating
  --spread-bins         Load warehouse bins and split each line across bins
  --bin-count <number>  Number of bins to use with --spread-bins (default: 6)
  --bin-entries <csv>   Explicit bin AbsEntry values to use for allocations
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

function readFirstWarehouse(lines) {
  const start = lines.findIndex((line) => line.trim() === "Warehouses:");
  if (start < 0) {
    return undefined;
  }

  for (let i = start + 1; i < lines.length; i += 1) {
    const line = lines[i];
    if (/^\S/.test(line) && line.trim().endsWith(":")) {
      break;
    }
    const match = line.match(/^\s{2}([^:#]+):\s*$/);
    if (match && !line.trim().startsWith("#")) {
      return match[1].trim();
    }
  }

  return undefined;
}

function loadSettings(configPath) {
  const text = fs.readFileSync(configPath, "utf8");
  const lines = text.split(/\r?\n/);
  const sbo = readIndentedBlock(lines, "SboSettings");
  const firstWarehouse = readFirstWarehouse(lines);

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
    firstWarehouse,
  };
}

function today() {
  return new Date().toISOString().slice(0, 10);
}

function splitQuantity(quantity, partCount) {
  if (Number.isInteger(quantity)) {
    const base = Math.floor(quantity / partCount);
    let remainder = quantity - (base * partCount);

    return Array.from({ length: partCount }, () => {
      const value = base + (remainder > 0 ? 1 : 0);
      remainder -= 1;
      return value;
    });
  }

  const scaledTotal = Math.round(quantity * 1000000);
  const base = Math.floor(scaledTotal / partCount);
  let remainder = scaledTotal - (base * partCount);

  return Array.from({ length: partCount }, () => {
    const scaled = base + (remainder > 0 ? 1 : 0);
    remainder -= 1;
    return scaled / 1000000;
  });
}

function buildPayload(args, warehouse, bins = []) {
  const date = today();
  return {
    CardCode: args.cardCode,
    DocDate: date,
    TaxDate: date,
    DocDueDate: date,
    Comments: args.comments,
    DocumentLines: args.items.map((itemCode, lineIndex) => {
      const line = {
        ItemCode: itemCode,
        Quantity: args.quantity,
        UnitPrice: args.unitPrice,
        WarehouseCode: warehouse,
        UseBaseUnits: "tYES",
      };

      if (bins.length > 0) {
        const quantities = splitQuantity(args.quantity, bins.length);
        line.DocumentLinesBinAllocations = bins.map((bin, allocationIndex) => ({
          BinAbsEntry: bin.absEntry,
          Quantity: quantities[allocationIndex],
          AllowNegativeQuantity: "tNO",
          SerialAndBatchNumbersBaseLine: -1,
          BaseLineNumber: lineIndex,
        }));
      }

      return line;
    }),
  };
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

function normalizeBin(value) {
  return {
    absEntry: value.AbsEntry ?? value.AbsoluteEntry ?? value.BinAbsEntry,
    binCode: value.BinCode ?? value.Code ?? String(value.AbsEntry ?? value.AbsoluteEntry ?? value.BinAbsEntry),
  };
}

async function loadBins(settings, cookie, warehouse, args) {
  if (args.binEntries) {
    return args.binEntries.map((absEntry) => ({
      absEntry,
      binCode: String(absEntry),
    }));
  }

  const query = `BinLocations?$select=AbsEntry,BinCode,Warehouse&$filter=Warehouse eq '${warehouse.replace(/'/g, "''")}'&$orderby=AbsEntry&$top=${args.binCount + 20}`;
  const response = await requestJson(
    "GET",
    `${settings.serviceLayerUrl}/b1s/v2/${query}`,
    null,
    { Cookie: cookie },
  );

  if (!response.ok) {
    throw new Error(`Failed to load bin locations for warehouse ${warehouse}: ${getErrorMessage(response)}`);
  }

  const values = Array.isArray(response.data?.value) ? response.data.value : [];
  const bins = values
    .map(normalizeBin)
    .filter((bin) => Number.isInteger(bin.absEntry))
    .filter((bin) => !/SYSTEM/i.test(bin.binCode));

  if (bins.length < args.binCount) {
    throw new Error(`Only found ${bins.length} bin location(s) for warehouse ${warehouse}; need ${args.binCount}`);
  }

  return bins.slice(0, args.binCount);
}

async function checkReference(settings, cookie, endpoint, label) {
  const response = await requestJson(
    "GET",
    `${settings.serviceLayerUrl}/b1s/v2/${endpoint}`,
    null,
    { Cookie: cookie },
  );

  if (!response.ok) {
    return {
      label,
      ok: false,
      message: getErrorMessage(response),
    };
  }

  return {
    label,
    ok: true,
    data: response.data,
  };
}

async function validateReferences(settings, cookie, args, warehouse, bins) {
  const checks = [];

  checks.push(await checkReference(settings, cookie, `BusinessPartners('${encodeURIComponent(args.cardCode)}')`, `BP ${args.cardCode}`));
  checks.push(await checkReference(settings, cookie, `Warehouses('${encodeURIComponent(warehouse)}')`, `Warehouse ${warehouse}`));

  for (const item of args.items) {
    checks.push(await checkReference(settings, cookie, `Items('${encodeURIComponent(item)}')`, `Item ${item}`));
  }

  for (const bin of bins) {
    checks.push(await checkReference(settings, cookie, `BinLocations(${bin.absEntry})`, `Bin ${bin.binCode} (${bin.absEntry})`));
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

async function main() {
  const args = parseArgs(process.argv.slice(2));
  const settings = loadSettings(args.config);
  const warehouse = args.warehouse || settings.firstWarehouse;

  if (!warehouse) {
    throw new Error("Warehouse could not be inferred from Configurations.yaml. Pass --warehouse <code>.");
  }

  console.log("Goods Receipt PO request");
  console.log(`  Config: ${args.config}`);
  console.log(`  Service Layer: ${settings.serviceLayerUrl}`);
  console.log(`  Company DB: ${settings.companyDb}`);
  console.log(`  User: ${settings.userName}`);
  console.log(`  Password: ${"*".repeat(settings.password.length)}`);
  console.log(`  Endpoint: PurchaseDeliveryNotes`);
  console.log(`  Warehouse: ${warehouse}`);
  console.log(`  Bin spread: ${args.spreadBins ? "yes" : "no"}`);

  let cookie = null;
  let bins = [];

  if (args.spreadBins || !args.dryRun || args.validateOnly) {
    const login = await requestJson("POST", `${settings.serviceLayerUrl}/b1s/v2/Login`, {
      CompanyDB: settings.companyDb,
      UserName: settings.userName,
      Password: settings.password,
    });

    if (!login.ok) {
      throw new Error(`Service Layer login failed: ${getErrorMessage(login)}`);
    }

    cookie = buildCookie(login);
  }

  if (args.spreadBins) {
    bins = await loadBins(settings, cookie, warehouse, args);
    console.log(`  Bins: ${bins.map((bin) => `${bin.binCode} (${bin.absEntry})`).join(", ")}`);
  }

  const payload = buildPayload(args, warehouse, bins);

  if (cookie && (args.spreadBins || args.validateOnly)) {
    const referencesOk = await validateReferences(settings, cookie, args, warehouse, bins);
    if (args.validateOnly) {
      if (!referencesOk) {
        process.exitCode = 1;
      }
      return;
    }
    if (!referencesOk) {
      throw new Error("Reference validation failed; not creating the GRPO");
    }
  }

  console.log("");
  console.log(JSON.stringify(payload, null, 2));

  if (args.dryRun) {
    console.log("\nDry run only. No document was created.");
    return;
  }

  const create = await requestJson(
    "POST",
    `${settings.serviceLayerUrl}/b1s/v2/PurchaseDeliveryNotes`,
    payload,
    { Cookie: cookie },
  );

  if (!create.ok) {
    throw new Error(`PurchaseDeliveryNotes creation failed: ${getErrorMessage(create)}`);
  }

  console.log("\nCreated Goods Receipt PO");
  console.log(`  DocEntry: ${create.data?.DocEntry ?? "N/A"}`);
  console.log(`  DocNum: ${create.data?.DocNum ?? "N/A"}`);
}

main().catch((error) => {
  console.error(`Error: ${error.message}`);
  process.exitCode = 1;
});
