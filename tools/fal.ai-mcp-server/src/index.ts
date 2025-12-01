#!/usr/bin/env node
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { pingTool } from "./tools/pingTool.js";

const server = new McpServer({
  name: "watchtower-tools",
  version: "1.0.0",
});

// Register the ping tool
server.tool(
  pingTool.name,
  pingTool.description,
  pingTool.inputSchema.shape,
  pingTool.handler
);

async function main() {
  const transport = new StdioServerTransport();

  // Handle graceful shutdown
  process.on("SIGINT", async () => {
    await server.close();
    process.exit(0);
  });

  process.on("SIGTERM", async () => {
    await server.close();
    process.exit(0);
  });

  // Connect and start the server
  await server.connect(transport);

  // Log to stderr so it doesn't interfere with stdio transport
  console.error("WatchTower MCP server started");
}

main().catch((error) => {
  console.error("Fatal error starting MCP server:", error);
  process.exit(1);
});
