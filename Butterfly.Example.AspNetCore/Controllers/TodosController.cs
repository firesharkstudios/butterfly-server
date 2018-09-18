using Butterfly.Core.Database.Memory;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Butterfly.Example.AspNetCore.Controllers
{
	[ApiController]
	public class TodosController : ControllerBase
	{
		private readonly MemoryDatabase _database;

		public TodosController(MemoryDatabase database)
		{
			_database = database;
		}

		[Route("api/todo/insert")]
		[HttpPost]
		public async Task<IActionResult> Post([FromBody] Todo todo)
		{
			var result = await _database.InsertAndCommitAsync<string>("todo", todo);
			return Ok(result);
		}

		[Route("api/todo/delete")]
		[HttpPost]
		public async Task<IActionResult> Delete([FromBody] string id)
		{
			await _database.DeleteAndCommitAsync("todo", id);
			return NoContent();
		}
	}

	public class Todo
	{
		public string name { get; set; }
	}
}
