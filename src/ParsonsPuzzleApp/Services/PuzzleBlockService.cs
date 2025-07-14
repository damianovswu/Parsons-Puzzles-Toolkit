namespace ParsonsPuzzleApp.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using ParsonsPuzzleApp.Data;
    using ParsonsPuzzleApp.Models;

    public class PuzzleBlockService : IPuzzleBlockService
    {
        private readonly ApplicationDbContext _context;

        public PuzzleBlockService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PuzzleBlock>> GetPuzzleBlocksAsync(int puzzleId)
        {
            return await _context.PuzzleBlocks
                .Where(pb => pb.PuzzleId == puzzleId)
                .Include(pb => pb.Lines)
                .OrderBy(pb => pb.OrderIndex)
                .ToListAsync();
        }

        public async Task<PuzzleBlock> CreatePuzzleBlockAsync(PuzzleBlock block)
        {
            _context.PuzzleBlocks.Add(block);
            await _context.SaveChangesAsync();
            return block;
        }

        public async Task UpdatePuzzleBlockAsync(PuzzleBlock block)
        {
            _context.PuzzleBlocks.Update(block);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePuzzleBlockAsync(int blockId)
        {
            var block = await _context.PuzzleBlocks.FindAsync(blockId);
            if (block != null)
            {
                _context.PuzzleBlocks.Remove(block);
                await _context.SaveChangesAsync();
            }
        }

        public string GenerateBlockConfiguration(List<PuzzleBlock> blocks)
        {
            var config = blocks.Select(b => new
            {
                groupId = b.GroupId,
                blockType = b.BlockType,
                orderMatters = !b.IsOrderIndependent,
                lines = b.Lines.OrderBy(l => l.LineOrder).Select(l => new
                {
                    content = l.Content,
                    optional = l.IsOptional
                }).ToList()
            });

            return JsonSerializer.Serialize(config);
        }
    }
}