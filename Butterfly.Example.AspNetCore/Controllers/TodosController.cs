using Butterfly.Core.Database.Memory;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Butterfly.Example.AspNetCore.Controllers
{
	[ApiController]
	[Route("api/{constoller}")]
	public class TodosController : ControllerBase
	{
		private readonly MemoryDatabase _database;

		public TodosController(MemoryDatabase database)
		{
			_database = database;
		}
		
		[HttpPost]
		public async Task<IActionResult> Post([FromBody] Todo todo)
		{
			var result = await _database.InsertAndCommitAsync<string>("todo", todo);
			return Ok(result);
		}
		
		[HttpDelete]
		public async Task<IActionResult> Delete([FromBody] string id)
		{
			await _database.DeleteAndCommitAsync("todo", id);
			return NoContent();
		}
	}

	public class Todo
	{
		public string Name { get; set; }
	}
}
