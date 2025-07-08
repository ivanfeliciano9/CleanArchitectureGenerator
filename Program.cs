// Console app que lê um arquivo Model.cs e gera estrutura CRUD baseada nele

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Reflection.Metadata;

namespace CrudGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            // Caminho do arquivo do modelo
            string modelPath = @"C:\Projeto_Sabiuz\SabiuzBackEnd\src\services\Sabiuz.Api\Models\Daily_Challenges.cs"; 
            string outputBase = @"GeneratedOutput";

            if (!File.Exists(modelPath))
            {
                Console.WriteLine("Arquivo Model.cs não encontrado.");
                return;
            }

            string modelContent = File.ReadAllText(modelPath);

            string className = Regex.Match(modelContent, @"class (\w+)").Groups[1].Value;
            var properties = new List<(string Type, string Name)>();

            foreach (Match match in Regex.Matches(modelContent, @"public (.*?) (.*?) \{ get; set; \}"))
            {
                string type = match.Groups[1].Value.Trim();
                string name = match.Groups[2].Value.Trim();
                if (!name.Equals("Id", StringComparison.OrdinalIgnoreCase) && !type.Contains("virtual"))
                    properties.Add((type, name));
            }

            string entityPath = Path.Combine(outputBase, "Application", className);
            Directory.CreateDirectory(Path.Combine(entityPath, $"Create{className}"));
            Directory.CreateDirectory(Path.Combine(entityPath, $"Delete{className}"));
            Directory.CreateDirectory(Path.Combine(entityPath, $"Update{className}"));
            Directory.CreateDirectory(Path.Combine(entityPath, "Dto"));
            Directory.CreateDirectory(Path.Combine(entityPath, "Request"));
            Directory.CreateDirectory(Path.Combine(outputBase, "Controllers"));

            GenerateDto(className, properties, entityPath);
            GenerateRequest(className, properties, entityPath);
            GenerateIQuery(className, entityPath);
            GenerateHandler(className, entityPath, properties);
            GenerateCreateCommand(className, properties, entityPath);
            GenerateCreateResult(className, entityPath);
            GenerateCreateValidation(className, entityPath);
            GenerateUpdateCommand(className, properties, entityPath);
            GenerateDeleteCommand(className, entityPath);
            GenerateController(className, properties, outputBase);

            Console.WriteLine($"✅ CRUD completo gerado com sucesso para a entidade: {className}.");
        }

        static void GenerateDeleteCommand(string className, string basePath)
        {
            string content = $"using MediatR;\n\nnamespace Sabiuz.Api.Application.{className}.Delete{className}\n{{\n    public class Delete{className}Command : IRequest<bool>\n    {{\n        public long Id {{ get; set; }}\n    }}\n}}";

            string dir = Path.Combine(basePath, $"Delete{className}");
            File.WriteAllText(Path.Combine(dir, $"Delete{className}Command.cs"), content);
        }

        static void GenerateUpdateCommand(string className, List<(string Type, string Name)> props, string basePath)
        {
            string content = $"using MediatR;\n\nnamespace Sabiuz.Api.Application.{className}.Update{className}\n{{\n    public class Update{className}Command : IRequest<bool>\n    {{\n        public int Id {{ get; set; }}\n";
            foreach (var p in props)
                content += $"        public {p.Type} {p.Name} {{ get; set; }}\n";
            content += "    }\n}";

            string dir = Path.Combine(basePath, $"Update{className}");
            File.WriteAllText(Path.Combine(dir, $"Update{className}Command.cs"), content);
        }

        static void GenerateCreateValidation(string className, string basePath)
        {
            string content = $"using FluentValidation;\n\nnamespace Sabiuz.Api.Application.{className}.Create{className}\n{{\n    public class Create{className}Validation : AbstractValidator<Create{className}Command>\n    {{\n        public Create{className}Validation()\n        {{\n            RuleFor(x => x).NotNull(); // Adicione regras de validação específicas aqui\n        }}\n    }}\n}}";

            string dir = Path.Combine(basePath, $"Create{className}");
            File.WriteAllText(Path.Combine(dir, $"Create{className}Validation.cs"), content);
        }

        static void GenerateCreateResult(string className, string basePath)
        {
            string content = $@"namespace Sabiuz.Api.Application.{className}.Create{className}
                        {{
                            public class Create{className}Result
                            {{
                                public long Id {{ get; set; }}

                                public Create{className}Result(long id)
                                {{
                                    Id = id;
                                }}
                            }}
                        }}";

            string dir = Path.Combine(basePath, $"Create{className}");
            File.WriteAllText(Path.Combine(dir, $"Create{className}Result.cs"), content);
        }

        static void GenerateCreateCommand(string className, List<(string Type, string Name)> props, string basePath)
        {
            string content = $"using MediatR;\n\nnamespace Sabiuz.Api.Application.{className}.Create{className}\n{{\n    public class Create{className}Command : IRequest<Create{className}Result>\n    {{\n";
            foreach (var p in props)
                content += $"        public {p.Type} {p.Name} {{ get; set; }}\n";
            content += "    }\n}";

            string dir = Path.Combine(basePath, $"Create{className}");
            File.WriteAllText(Path.Combine(dir, $"Create{className}Command.cs"), content);
        }

        static void GenerateIQuery(string className, string basePath)
        {
            var builder = new System.Text.StringBuilder();

            builder.AppendLine("using Sabiuz.Core.Data;");
            builder.AppendLine("using Sabiuz.Api.Application." + className + ".Dto;");
            builder.AppendLine("using Mapster;");
            builder.AppendLine("using Microsoft.EntityFrameworkCore;");
            builder.AppendLine();
            builder.AppendLine("namespace  Sabiuz.Api.Application." + className);
            builder.AppendLine("{");
            builder.AppendLine("    public interface IQuery" + className);
            builder.AppendLine("    {");
            builder.AppendLine("        Task<" + className + "Dto?> GetByIdAsync(long id);");
            builder.AppendLine("        Task<PaginatedList<" + className + "Dto>> GetAllAsync(int pageNumber, int pageSize);");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    public class Query" + className + " : IQuery" + className);
            builder.AppendLine("    {");
            builder.AppendLine("        private readonly IRepositoryConsult<Core.Models." + className + "> _repository;");
            builder.AppendLine();
            builder.AppendLine("        public Query" + className + "(IRepositoryConsult<Core.Models." + className + "> repository)");
            builder.AppendLine("        {");
            builder.AppendLine("            _repository = repository;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        public async Task<" + className + "Dto?> GetByIdAsync(long id)");
            builder.AppendLine("        {");
            builder.AppendLine("            var result = await _repository.GetByIdAsync(id);");
            builder.AppendLine("            return result?.Adapt<" + className + "Dto>();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        public async Task<PaginatedList<" + className + "Dto>> GetAllAsync(int pageNumber, int pageSize)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (pageSize > 100) pageSize = 100;");
            builder.AppendLine("            var query = _repository.GetQueryable().AsNoTracking().ProjectToType<" + className + "Dto>();");
            builder.AppendLine("            return await PaginatedList<" + className + "Dto>.CreateAsync(query, pageNumber, pageSize);");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            File.WriteAllText(Path.Combine(basePath, "IQuery" + className + ".cs"), builder.ToString());
        }  

        static void GenerateRequest(string className, List<(string Type, string Name)> props, string basePath)
        {
            string content = $"namespace Sabiuz.Api.Application.{className}.Request\n{{\n    public class {className}Request\n    {{\n";
            foreach (var p in props)
                content += $"        public {p.Type} {p.Name} {{ get; set; }}\n";
            content += "    }\n}";

            string dir = Path.Combine(basePath, "Request");
            File.WriteAllText(Path.Combine(dir, $"{className}Request.cs"), content);
        }

        static void GenerateDto(string className, List<(string Type, string Name)> props, string basePath)
        {
            string dto = $"namespace Sabiuz.Api.Application.{className}.Dto\n{{\n    public class {className}Dto\n    {{\n        public long Id {{ get; set; }}\n";
            foreach (var p in props)
                dto += $"        public {p.Type} {p.Name} {{ get; set; }}\n";
            dto += "    }\n}";
            File.WriteAllText(Path.Combine(basePath,"Dto", $"{className}Dto.cs"), dto);
        }

        static void GenerateHandler(string className, string entityPath, List<(string Type, string Name)> properties)
        {
            var handlerBuilder = new System.Text.StringBuilder();

            handlerBuilder.AppendLine($"using MediatR;");
            handlerBuilder.AppendLine($"using Sabiuz.Api.Application.{className}.Create{className};");
            handlerBuilder.AppendLine($"using Sabiuz.Api.Application.{className}.Update{className};");
            handlerBuilder.AppendLine($"using Sabiuz.Api.Application.{className}.Delete{className};");
            handlerBuilder.AppendLine("using Sabiuz.Core.Data;");
            handlerBuilder.AppendLine("using Sabiuz.Core.Models;");
            handlerBuilder.AppendLine("using Microsoft.Extensions;");
            handlerBuilder.AppendLine();
            handlerBuilder.AppendLine($"namespace Sabiuz.Api.Application.{className}");
            handlerBuilder.AppendLine("{");
            handlerBuilder.AppendLine($"    public class {className}Handler :");
            handlerBuilder.AppendLine($"        IRequestHandler<Create{className}Command, Create{className}Result>,");
            handlerBuilder.AppendLine($"        IRequestHandler<Update{className}Command, bool>,");
            handlerBuilder.AppendLine($"        IRequestHandler<Delete{className}Command, bool>");
            handlerBuilder.AppendLine("    {");
            handlerBuilder.AppendLine($"        private readonly IBaseRepository<Core.Models.{className}> _repository;");
            handlerBuilder.AppendLine();
            handlerBuilder.AppendLine($"        public {className}Handler(IBaseRepository<Core.Models.{className}> repository)");
            handlerBuilder.AppendLine("        {");
            handlerBuilder.AppendLine($"            _repository = repository;");
            handlerBuilder.AppendLine("        }");

            // CREATE
            handlerBuilder.AppendLine();
            handlerBuilder.AppendLine($"        public async Task<Create{className}Result> Handle(Create{className}Command request, CancellationToken cancellationToken)");
            handlerBuilder.AppendLine("        {");
            handlerBuilder.AppendLine($"            var entity = new Core.Models.{className}();");
            foreach (var prop in properties)
            {
                if (prop.Type.Contains("DateTime"))
                    handlerBuilder.AppendLine($"            entity.{prop.Name} = request.{prop.Name}.EnsureUtc();");
                else
                    handlerBuilder.AppendLine($"            entity.{prop.Name} = request.{prop.Name};");
            }

            handlerBuilder.AppendLine();
            handlerBuilder.AppendLine($"            await _repository.AddAsync(entity, cancellationToken);");
            handlerBuilder.AppendLine($"            await _repository.UnitOfWork.CommitAsync();");
            handlerBuilder.AppendLine($"            return new Create{className}Result(entity.Id);");
            handlerBuilder.AppendLine("        }");

            // UPDATE
            handlerBuilder.AppendLine();
            handlerBuilder.AppendLine($"        public async Task<bool> Handle(Update{className}Command request, CancellationToken cancellationToken)");
            handlerBuilder.AppendLine("        {");
            handlerBuilder.AppendLine($"            var entityList = await _repository.RepositoryConsult.SearchAsync(x => x.Id == request.Id);");
            handlerBuilder.AppendLine($"            var entity = entityList.FirstOrDefault();");
            handlerBuilder.AppendLine($"            if (entity == null) return false;");
            
            foreach (var prop in properties)
            {
                if (prop.Type.Contains("DateTime"))
                    handlerBuilder.AppendLine($"            entity.{prop.Name} = request.{prop.Name}.EnsureUtc();");
                else
                    handlerBuilder.AppendLine($"            entity.{prop.Name} = request.{prop.Name};");
            }

            handlerBuilder.AppendLine($"            _repository.Update(entity);");
            handlerBuilder.AppendLine($"            return await _repository.UnitOfWork.CommitAsync();");
            handlerBuilder.AppendLine("        }");

            // DELETE
            handlerBuilder.AppendLine();
            handlerBuilder.AppendLine($"        public async Task<bool> Handle(Delete{className}Command request, CancellationToken cancellationToken)");
            handlerBuilder.AppendLine("        {");
            handlerBuilder.AppendLine($"            var entity = await _repository.RepositoryConsult.SearchAsync(x => x.Id == request.Id);");
            handlerBuilder.AppendLine($"            var toDelete = entity.FirstOrDefault();");
            handlerBuilder.AppendLine($"            if (toDelete == null) return false;");
            handlerBuilder.AppendLine($"            _repository.Remove(toDelete);");
            handlerBuilder.AppendLine($"            await _repository.UnitOfWork.CommitAsync();");
            handlerBuilder.AppendLine($"            return true;");
            handlerBuilder.AppendLine("        }");

            handlerBuilder.AppendLine("    }");
            handlerBuilder.AppendLine("}");

            string handlerPath = Path.Combine(entityPath, $"{className}Handler.cs");
            File.WriteAllText(handlerPath, handlerBuilder.ToString());
        }

        static void GenerateController(string className, List<(string Type, string Name)> properties, string basePath)
        {
            string propAssignments = string.Join(",\n", properties.Select(p => $"                {p.Name} = request.{p.Name}"));

            string content = $@"using MediatR;
                                using Microsoft.AspNetCore.Mvc;
                                using Sabiuz.Core.Web;
                                using Sabiuz.Api.Application.{className};
                                using Sabiuz.Api.Application.{className}.Dto;
                                using Sabiuz.Api.Application.{className}.Request;
                                using Sabiuz.Api.Application.{className}.Create{className};
                                using Sabiuz.Api.Application.{className}.Update{className};
                                using Sabiuz.Api.Application.{className}.Delete{className};

                                namespace Sabiuz.Api.Controllers
                                {{
                                    //[ApiController]
                                    //[Route(\""api / [controller]\"")]
                                    public class {className}Controller : BaseController
                                    {{
                                        private readonly IMediator _mediator;
                                        private readonly IQuery{className} _query;

                                        public {className}Controller(IMediator mediator, IQuery{className} query)
                                        {{
                                            _mediator = mediator;
                                            _query = query;
                                        }}                                       

                                        [HttpGet]
                                        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
                                        {{
                                            var result = await _query.GetAllAsync(pageNumber, pageSize);
                                            return Ok(result);
                                        }}

                                        [HttpGet(""{{Id}}"")]
                                        public async Task<IActionResult> GetById(long Id)
                                        {{
                                            {{
                                                var result = await _query.GetByIdAsync(Id);
                                                if (result == null) return NotFound();
                                                return Ok(result);
                                            }}
                                        }}

                                        [HttpPost]
                                        public async Task<IActionResult> Create([FromBody] {className}Request request)
                                        {{
                                            var command = new Create{className}Command

                                            {{
                                            {propAssignments}
                                            }};

                                            var result = await _mediator.Send(command);
                                            return Ok(result);
                                        }}

                                        [HttpPut]
                                        public async Task<IActionResult> Update([FromBody] {className}Request request)
                                        {{
                                            var command = new Update{className}Command

                                            {{
                                            {propAssignments}
                                            }};

                                            var result = await _mediator.Send(command);
                                            return Ok(result);
                                        }}

                                        [HttpDelete(""{{id}}"")]
                                        public async Task<IActionResult> Delete(long id)
                                        {{
                                            {{
                                                var command = new Delete{className}Command {{ Id = id }};
                                                var result = await _mediator.Send(command);
                                                return Ok(result);
                                            }}
                                        }}
                                    }}
                                }}";

            File.WriteAllText(Path.Combine(basePath, "Controllers", $"{className}Controller.cs"), content);
        }
    }
}
