# QBD MCP Server

MCP server that connects Claude Desktop to QuickBooks Desktop.

## Prerequisites

- Windows PC with QuickBooks Desktop installed
- Claude Desktop (or Claude Code)

## Setup

1. Download `QbdMcp.Server.exe` from [Releases](../../releases)
2. Run it once — a `config.json` file will be created next to the exe
3. Edit `config.json` to add your company files:
   ```json
   {
     "companyFiles": [
       { "name": "My Company", "path": "C:\\path\\to\\company.qbw" }
     ]
   }
   ```
4. Add to your Claude Desktop config (`%APPDATA%\Claude\claude_desktop_config.json`):
   ```json
   {
     "mcpServers": {
       "quickbooks": {
         "command": "C:\\path\\to\\QbdMcp.Server.exe"
       }
     }
   }
   ```
5. Open QuickBooks Desktop, then open Claude Desktop
6. First run: QuickBooks will ask to authorize the app — click "Yes, always allow"

## Building from Source

Requires .NET 8 SDK and QuickBooks Desktop SDK (QBFC13) installed.

```bash
dotnet publish src/QbdMcp/QbdMcp.csproj -c Release
```

Output: `src/QbdMcp/bin/Release/net8.0-windows/win-x64/publish/QbdMcp.exe`

## Available Tools (24)

### Connection Management
| Tool | Description |
|------|-------------|
| `ListCompanyFiles` | List available company files from config |
| `SwitchCompanyFile` | Switch to a different company file |

### Lookups
| Tool | Description |
|------|-------------|
| `ListCustomers` | List customers |
| `GetCustomer` | Search customer by name |
| `ListVendors` | List vendors |
| `GetVendor` | Search vendor by name |
| `ListInvoices` | List recent invoices |
| `GetOverdueInvoices` | Unpaid invoices past due date |
| `ListAccounts` | Chart of accounts with balances |
| `GetAccountBalance` | Balance for a specific account |
| `ListBills` | List bills with vendor/status filters |
| `GetClientSummary` | AR/AP totals, overdue count |
| `SearchTransactions` | Search across invoices, bills, checks, sales receipts |

### Data Entry
| Tool | Description |
|------|-------------|
| `CreateBill` | Enter a bill with expense line items |
| `PayBill` | Pay a bill by cheque |
| `CreateInvoice` | Create an invoice with line items |
| `ReceivePayment` | Record a customer payment |
| `MakeCheque` | Write a cheque for direct payment |
| `CreateSalesReceipt` | Record a direct sale |
| `CreateJournalEntry` | Create a general journal entry |

### Edit/Void
| Tool | Description |
|------|-------------|
| `VoidTransaction` | Void a transaction |
| `DeleteTransaction` | Delete a transaction |

### Reports
| Tool | Description |
|------|-------------|
| `GetTrialBalance` | Trial balance for a date range |
| `GetGeneralLedger` | General ledger for a date range |
