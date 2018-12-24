using Microsoft.AspNetCore.Mvc;
using EnsureThat;
using Pushover.Dto;
using Pushover.Service;
using Pushover.Components;
using System;

namespace Pushover.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {

        private readonly IMessageService service;

        public MessageController(IMessageService service)
        {
            this.service = service;
        }

        // POST api/values
        [HttpPost]
        public IActionResult Post([FromBody] PushMessage message)
        {
            EnsureArg.IsNotNull(message);

            if (!IsMessageValid(message))
            {
                return BadRequest();
            }

            try
            {
                service.SendMessage(message);
                return NoContent();
            }
            catch(BadParametersException ex)
            {
                return BadRequest(ex.Message);
            }
            catch(EntityNotFoundException ex)
            {
                return NotFound();
            }
            catch(InvalidOperationException ex)
            {
                return StatusCode(500);
            }
        }

        private bool IsMessageValid(PushMessage message)
        {
            if (string.IsNullOrEmpty(message.token) || string.IsNullOrEmpty(message.message) || string.IsNullOrEmpty(message.token))
            {
                return false;
            }

            return true;
        }
    }
}
