using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ControllerListController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetControllers()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var controllers = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                           (t.IsSubclassOf(typeof(ControllerBase)) || t.Name.EndsWith("Controller")))
                .Where(t => t.Name != nameof(ControllerListController))
                .ToList();

            List<ControllerInfo> controllerInfos = [];

            foreach (var controller in controllers)
            {
                var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName)
                    .ToList();

                List<ActionInfo> actionInfos = [];

                foreach (var action in actions)
                {
                    List<string> roles = [];

                    // Explicitly cast the attributes using Cast<AuthorizeAttribute>()
                    var authorizeAttributes = action.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                                                   .Cast<AuthorizeAttribute>();

                    foreach (var attr in authorizeAttributes)
                    {
                        if (!string.IsNullOrEmpty(attr.Roles))
                        {
                            roles.AddRange(attr.Roles.Split(',')
                                .Select(r => r.Trim())
                                .Where(r => !string.IsNullOrEmpty(r)));
                        }
                    }

                    actionInfos.Add(new ActionInfo
                    {
                        Name = action.Name,
                        Roles = roles
                    });
                }

                controllerInfos.Add(new ControllerInfo
                {
                    Name = controller.Name,
                    Actions = actionInfos
                });
            }

            controllerInfos = [.. controllerInfos.OrderBy(c => c.Name == "UserController" ? 0 : 1)];

            return Ok(controllerInfos);
        }
    }

    // Kontrolcü bilgilerini temsil eden sınıf
    public class ControllerInfo
    {
        public string Name { get; set; } = string.Empty;
        public List<ActionInfo> Actions { get; set; } =[];
    }

    // Aksiyon bilgilerini temsil eden sınıf
    public class ActionInfo
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = [];
    }
}