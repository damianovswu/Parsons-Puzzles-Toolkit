namespace ParsonsPuzzleApp.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ParsonsPuzzleApp.Models;

    public interface IPuzzleBlockService
    {
        Task<List<PuzzleBlock>> GetPuzzleBlocksAsync(int puzzleId);
        Task<PuzzleBlock> CreatePuzzleBlockAsync(PuzzleBlock block);
        Task UpdatePuzzleBlockAsync(PuzzleBlock block);
        Task DeletePuzzleBlockAsync(int blockId);
        string GenerateBlockConfiguration(List<PuzzleBlock> blocks);
    }
}