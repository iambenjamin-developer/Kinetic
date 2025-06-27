//using Inventory.API.Producers;
//using MassTransit;
//using Microsoft.AspNetCore.Mvc;

//namespace Inventory.API.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class ValuesController : ControllerBase
//    {
//        private readonly IPublishEndpoint _publishEndpoint;
//        private readonly QueueProducerService _queueProducerService;

//        public ValuesController(IPublishEndpoint publishEndpoint, QueueProducerService queueProducerService)
//        {
//            _publishEndpoint = publishEndpoint;
//            _queueProducerService = queueProducerService;
//        }

//        // POST /api/products
//        [HttpGet("SendEvent")]
//        public async Task<ActionResult> SendEvent()
//        {
//            await _queueProducerService.SendSubscribeProductEvent();

//            return Ok();
//        }



//        // GET: api/<ValuesController>
//        [HttpGet]
//        public IEnumerable<string> Get()
//        {
//            return new string[] { "value1", "value2" };
//        }

//        // GET api/<ValuesController>/5
//        [HttpGet("{id}")]
//        public string Get(int id)
//        {
//            return "value";
//        }

//        // POST api/<ValuesController>
//        [HttpPost]
//        public void Post([FromBody] string value)
//        {
//        }

//        // PUT api/<ValuesController>/5
//        [HttpPut("{id}")]
//        public void Put(int id, [FromBody] string value)
//        {
//        }

//        // DELETE api/<ValuesController>/5
//        [HttpDelete("{id}")]
//        public void Delete(int id)
//        {
//        }
//    }
//}
