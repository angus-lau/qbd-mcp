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

## Available Tools

24 tools across 6 categories: connection management, lookups, data entry, edit/void, search, and reports. Ask Claude "what can you do with QuickBooks?" to see the full list.
