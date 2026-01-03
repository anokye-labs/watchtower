#!/usr/bin/env node
/**
 * Tests for GraphQL Client pagination
 * Run with: node --test graphql-client.test.mjs
 */

import { describe, it, mock } from 'node:test';
import assert from 'node:assert';
import { ProjectGraphQLClient } from './graphql-client.mjs';

describe('ProjectGraphQLClient', () => {
  describe('getProjectItemId', () => {
    it('should find item in first page when project has < 100 items', async () => {
      const mockGraphql = mock.fn(async (query, variables) => {
        return {
          node: {
            items: {
              pageInfo: {
                hasNextPage: false,
                endCursor: null
              },
              nodes: [
                { id: 'item1', content: { id: 'issue1' } },
                { id: 'item2', content: { id: 'issue2' } },
                { id: 'item3', content: { id: 'issue3' } }
              ]
            }
          }
        };
      });

      const client = new ProjectGraphQLClient('fake-token', 'fake-project-id');
      client.octokit.graphql = mockGraphql;

      const result = await client.getProjectItemId('issue2');

      assert.strictEqual(result, 'item2');
      assert.strictEqual(mockGraphql.mock.calls.length, 1);
      assert.strictEqual(mockGraphql.mock.calls[0].arguments[1].cursor, null);
    });

    it('should find item on second page when paginating', async () => {
      let callCount = 0;
      const mockGraphql = mock.fn(async (query, variables) => {
        callCount++;
        if (callCount === 1) {
          // First page - no match
          return {
            node: {
              items: {
                pageInfo: {
                  hasNextPage: true,
                  endCursor: 'cursor1'
                },
                nodes: [
                  { id: 'item1', content: { id: 'issue1' } },
                  { id: 'item2', content: { id: 'issue2' } }
                ]
              }
            }
          };
        } else {
          // Second page - match found
          return {
            node: {
              items: {
                pageInfo: {
                  hasNextPage: false,
                  endCursor: 'cursor2'
                },
                nodes: [
                  { id: 'item3', content: { id: 'issue3' } },
                  { id: 'item4', content: { id: 'issue4' } }
                ]
              }
            }
          };
        }
      });

      const client = new ProjectGraphQLClient('fake-token', 'fake-project-id');
      client.octokit.graphql = mockGraphql;

      const result = await client.getProjectItemId('issue4');

      assert.strictEqual(result, 'item4');
      assert.strictEqual(mockGraphql.mock.calls.length, 2);
      assert.strictEqual(mockGraphql.mock.calls[0].arguments[1].cursor, null);
      assert.strictEqual(mockGraphql.mock.calls[1].arguments[1].cursor, 'cursor1');
    });

    it('should paginate through multiple pages (>100 items)', async () => {
      let callCount = 0;
      const mockGraphql = mock.fn(async (query, variables) => {
        callCount++;
        if (callCount === 1) {
          // First page (100 items)
          return {
            node: {
              items: {
                pageInfo: {
                  hasNextPage: true,
                  endCursor: 'cursor1'
                },
                nodes: Array.from({ length: 100 }, (_, i) => ({
                  id: `item${i}`,
                  content: { id: `issue${i}` }
                }))
              }
            }
          };
        } else if (callCount === 2) {
          // Second page (100 items)
          return {
            node: {
              items: {
                pageInfo: {
                  hasNextPage: true,
                  endCursor: 'cursor2'
                },
                nodes: Array.from({ length: 100 }, (_, i) => ({
                  id: `item${i + 100}`,
                  content: { id: `issue${i + 100}` }
                }))
              }
            }
          };
        } else {
          // Third page (50 items, last page)
          return {
            node: {
              items: {
                pageInfo: {
                  hasNextPage: false,
                  endCursor: 'cursor3'
                },
                nodes: Array.from({ length: 50 }, (_, i) => ({
                  id: `item${i + 200}`,
                  content: { id: `issue${i + 200}` }
                }))
              }
            }
          };
        }
      });

      const client = new ProjectGraphQLClient('fake-token', 'fake-project-id');
      client.octokit.graphql = mockGraphql;

      // Find item on third page
      const result = await client.getProjectItemId('issue225');

      assert.strictEqual(result, 'item225');
      assert.strictEqual(mockGraphql.mock.calls.length, 3);
      assert.strictEqual(mockGraphql.mock.calls[0].arguments[1].cursor, null);
      assert.strictEqual(mockGraphql.mock.calls[1].arguments[1].cursor, 'cursor1');
      assert.strictEqual(mockGraphql.mock.calls[2].arguments[1].cursor, 'cursor2');
    });

    it('should return null when item not found after checking all pages', async () => {
      let callCount = 0;
      const mockGraphql = mock.fn(async (query, variables) => {
        callCount++;
        if (callCount === 1) {
          return {
            node: {
              items: {
                pageInfo: {
                  hasNextPage: true,
                  endCursor: 'cursor1'
                },
                nodes: [
                  { id: 'item1', content: { id: 'issue1' } },
                  { id: 'item2', content: { id: 'issue2' } }
                ]
              }
            }
          };
        } else {
          return {
            node: {
              items: {
                pageInfo: {
                  hasNextPage: false,
                  endCursor: 'cursor2'
                },
                nodes: [
                  { id: 'item3', content: { id: 'issue3' } }
                ]
              }
            }
          };
        }
      });

      const client = new ProjectGraphQLClient('fake-token', 'fake-project-id');
      client.octokit.graphql = mockGraphql;

      const result = await client.getProjectItemId('issue-not-exists');

      assert.strictEqual(result, null);
      assert.strictEqual(mockGraphql.mock.calls.length, 2);
    });

    it('should return null for empty project', async () => {
      const mockGraphql = mock.fn(async (query, variables) => {
        return {
          node: {
            items: {
              pageInfo: {
                hasNextPage: false,
                endCursor: null
              },
              nodes: []
            }
          }
        };
      });

      const client = new ProjectGraphQLClient('fake-token', 'fake-project-id');
      client.octokit.graphql = mockGraphql;

      const result = await client.getProjectItemId('issue1');

      assert.strictEqual(result, null);
      assert.strictEqual(mockGraphql.mock.calls.length, 1);
    });

    it('should handle project with exactly 100 items', async () => {
      const mockGraphql = mock.fn(async (query, variables) => {
        return {
          node: {
            items: {
              pageInfo: {
                hasNextPage: false,
                endCursor: 'cursor1'
              },
              nodes: Array.from({ length: 100 }, (_, i) => ({
                id: `item${i}`,
                content: { id: `issue${i}` }
              }))
            }
          }
        };
      });

      const client = new ProjectGraphQLClient('fake-token', 'fake-project-id');
      client.octokit.graphql = mockGraphql;

      const result = await client.getProjectItemId('issue99');

      assert.strictEqual(result, 'item99');
      assert.strictEqual(mockGraphql.mock.calls.length, 1);
    });

    it('should stop pagination when item is found', async () => {
      let callCount = 0;
      const mockGraphql = mock.fn(async (query, variables) => {
        callCount++;
        if (callCount === 1) {
          // First page - no match
          return {
            node: {
              items: {
                pageInfo: {
                  hasNextPage: true,
                  endCursor: 'cursor1'
                },
                nodes: [
                  { id: 'item1', content: { id: 'issue1' } }
                ]
              }
            }
          };
        } else if (callCount === 2) {
          // Second page - match found, should not continue to page 3
          return {
            node: {
              items: {
                pageInfo: {
                  hasNextPage: true,  // There IS a next page
                  endCursor: 'cursor2'
                },
                nodes: [
                  { id: 'item2', content: { id: 'issue2' } },
                  { id: 'item3', content: { id: 'issue-target' } }
                ]
              }
            }
          };
        } else {
          // Third page - should never be called
          throw new Error('Should not paginate beyond finding the item');
        }
      });

      const client = new ProjectGraphQLClient('fake-token', 'fake-project-id');
      client.octokit.graphql = mockGraphql;

      const result = await client.getProjectItemId('issue-target');

      assert.strictEqual(result, 'item3');
      assert.strictEqual(mockGraphql.mock.calls.length, 2, 'Should stop after finding item');
    });

    it('should handle null content in items gracefully', async () => {
      const mockGraphql = mock.fn(async (query, variables) => {
        return {
          node: {
            items: {
              pageInfo: {
                hasNextPage: false,
                endCursor: null
              },
              nodes: [
                { id: 'item1', content: null },  // Null content
                { id: 'item2', content: { id: 'issue2' } },
                { id: 'item3', content: {} }  // Empty content without id
              ]
            }
          }
        };
      });

      const client = new ProjectGraphQLClient('fake-token', 'fake-project-id');
      client.octokit.graphql = mockGraphql;

      const result = await client.getProjectItemId('issue2');

      assert.strictEqual(result, 'item2');
      assert.strictEqual(mockGraphql.mock.calls.length, 1);
    });
  });
});
