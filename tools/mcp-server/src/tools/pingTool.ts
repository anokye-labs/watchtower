import { z } from "zod";

export const pingTool = {
  name: "ping",
  description: "A simple ping tool for testing MCP server connectivity",
  inputSchema: z.object({
    message: z
      .string()
      .optional()
      .describe("Optional message to include in the pong response"),
  }),
  handler: async (input: { message?: string }) => {
    if (input.message) {
      return {
        content: [
          {
            type: "text" as const,
            text: `pong: ${input.message}`,
          },
        ],
      };
    }
    return {
      content: [
        {
          type: "text" as const,
          text: "pong",
        },
      ],
    };
  },
};
