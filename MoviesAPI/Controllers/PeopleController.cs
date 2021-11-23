using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesAPI.DTOs;
using MoviesAPI.Entities;
using MoviesAPI.Helpers;
using MoviesAPI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MoviesAPI.Controllers
{
    [ApiController]
    [Route("api/people")]
    [EnableCors(PolicyName = "AllowAPIRequestIO")]
    public class PeopleController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IFileStorageService fileStorageService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly string containerName = "people";

        public PeopleController(ApplicationDbContext context,
            IMapper mapper,
            IFileStorageService fileStorageService,
            UserManager<IdentityUser> userManager
            )
        {
            this.context = context;
            this.mapper = mapper;
            this.fileStorageService = fileStorageService;
            _userManager = userManager;
        }

        [HttpGet]
        [EnableCors(PolicyName = "AllowAPIRequestIO")]
        public async Task<ActionResult<List<PersonDTO>>> Get()
        {

            var people = await context.People
                .ToListAsync();

            //return result;
            var result = new List<PersonDTO>();
            result = mapper.Map<List<PersonDTO>>(people);
            return result;
        }

        //[HttpGet]
        //public async Task<ActionResult<List<PersonDTO>>> Get([FromQuery] PaginationDTO pagination)
        //{
        //    var queryable = context.People.AsQueryable();
        //    await HttpContext.InsertPaginationParametersInResponse(queryable, pagination.RecordsPerPage);
        //    var people = await queryable.Paginate(pagination).ToListAsync();
        //    return mapper.Map<List<PersonDTO>>(people);
        //}

        [HttpGet("{id}", Name = "getPerson")]
        public async Task<ActionResult<PersonDTO>> Get(string id)
        {
            var person = await context.People.FirstOrDefaultAsync(x => x.Id == id);

            if (person == null)
            {
                return NotFound();
            }

            return mapper.Map<PersonDTO>(person);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult> Post([FromForm] PersonCreationDTO personCreationDTO)
        {
            var person = mapper.Map<Person>(personCreationDTO);

            //if (personCreationDTO.Picture != null)
            //{
            //    using (var memoryStream = new MemoryStream())
            //    {
            //        await personCreationDTO.Picture.CopyToAsync(memoryStream);
            //        var content = memoryStream.ToArray();
            //        var extension = Path.GetExtension(personCreationDTO.Picture.FileName);
            //        person.Picture =
            //            await fileStorageService.SaveFile(content, extension, containerName,
            //                                                personCreationDTO.Picture.ContentType);
            //    }
            //}

            context.Add(person);
            await context.SaveChangesAsync();
            var personDTO = mapper.Map<PersonDTO>(person);
            return new CreatedAtRouteResult("getPerson", new { id = person.Id }, personDTO);
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //For Both Actors and Admins
        public async Task<ActionResult> Put(string id, [FromBody] PersonCreationDTO personCreationDTO)
        {
            var personDB = await context.People.FirstOrDefaultAsync(x => x.Id == id);

            if (personDB == null) { return NotFound(); }

            personDB.Name = personCreationDTO.Name;
            personDB.Biography = personCreationDTO.Biography;
            personDB.DateOfBirth = personCreationDTO.DateOfBirth;


            //personDB = mapper.Map(personCreationDTO, personDB);

            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpPatch("{id}")]
        [EnableCors(PolicyName = "AllowAPIRequestIO")]
        public async Task<ActionResult> Patch(string id, [FromBody] JsonPatchDocument<PersonPatchDTO> patchDocument)
        {
            if (patchDocument == null)
            {
                return BadRequest();
            }

            var entityFromDB = await context.People.FirstOrDefaultAsync(x => x.Id == id);

            if (entityFromDB == null)
            {
                return NotFound();
            }

            var entityDTO = mapper.Map<PersonPatchDTO>(entityFromDB);

            patchDocument.ApplyTo(entityDTO, ModelState);

            var isValid = TryValidateModel(entityDTO);

            if (!isValid)
            {
                return BadRequest(ModelState);
            }

            mapper.Map(entityDTO, entityFromDB);

            await context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        //[EnableCors(PolicyName = "AllowAPIRequestIO")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //[DisableCors]
        public async Task<ActionResult> Delete(string id)
        {
            var exists = await context.People.AnyAsync(x => x.Id == id);
            if (!exists)
            {
                return NotFound();
            }

            context.Remove(new Person() { Id = id });
            var user = await _userManager.FindByIdAsync(id);
            await _userManager.DeleteAsync(user);
            await context.SaveChangesAsync();

            return NoContent();
        }
    }
}
