using AutoMapper;
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
using System.Linq.Dynamic.Core;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;

namespace MoviesAPI.Controllers
{
    [ApiController]
    [Route("api/movies")]
    [EnableCors(PolicyName = "AllowAPIRequestIO")]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IFileStorageService fileStorageService;
        private readonly ILogger<MoviesController> logger;
        private readonly string containerName = "movies";

        public MoviesController(ApplicationDbContext context,
            IMapper mapper,
            IFileStorageService fileStorageService,
            ILogger<MoviesController> logger)
        {
            this.context = context;
            this.mapper = mapper;
            this.fileStorageService = fileStorageService;
            this.logger = logger;
        }

        [HttpGet]
        [EnableCors(PolicyName = "AllowAPIRequestIO")]
        public async Task<ActionResult<List<MovieDTO>>> Get()
        {
            var top = 7;
            var today = DateTime.Today;
            //var upcomingReleases = await context.Movies
            //    .Where(x => x.ReleaseDate > today)
            //    .OrderBy(x => x.ReleaseDate)
            //    .Take(top)
            //    .ToListAsync();

            var allMovies = await context.Movies
                .Take(top)
                .ToListAsync();

            //var result = new IndexMoviePageDTO();
            //result.InTheaters = mapper.Map<List<MovieDTO>>(inTheaters);
            //result.UpcomingReleases = mapper.Map<List<MovieDTO>>(upcomingReleases);

            //return result;
            var result = new List<MovieDTO>();
            result = mapper.Map<List<MovieDTO>>(allMovies);
            return result;
        }

        [HttpGet("filter")]
        [EnableCors(PolicyName = "AllowAPIRequestIO")]
        public async Task<ActionResult<List<MovieDTO>>> Filter([FromQuery] FilterMoviesDTO filterMoviesDTO)
        {
            var moviesQueryable = context.Movies.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filterMoviesDTO.Title))
            {
                moviesQueryable = moviesQueryable.Where(x => x.Title.Contains(filterMoviesDTO.Title));
            }

            if (filterMoviesDTO.InTheaters)
            {
                moviesQueryable = moviesQueryable.Where(x => x.InTheaters);
            }

            if (filterMoviesDTO.UpcomingReleases)
            {
                var today = DateTime.Today;
                moviesQueryable = moviesQueryable.Where(x => x.ReleaseDate > today);
            }



            if (!string.IsNullOrWhiteSpace(filterMoviesDTO.OrderingField))
            {
                try
                {
                    moviesQueryable = moviesQueryable
                        .OrderBy($"{filterMoviesDTO.OrderingField} {(filterMoviesDTO.AscendingOrder ? "ascending" : "descending")}");
                }
                catch
                {
                    // log this
                    logger.LogWarning("Could not order by field: " + filterMoviesDTO.OrderingField);
                }
            }

            await HttpContext.InsertPaginationParametersInResponse(moviesQueryable,
                filterMoviesDTO.RecordsPerPage);

            var movies = await moviesQueryable.Paginate(filterMoviesDTO.Pagination).ToListAsync();

            return mapper.Map<List<MovieDTO>>(movies);
        }

        [HttpGet("{id}", Name = "getMovie")]
        [EnableCors(PolicyName = "AllowAPIRequestIO")]
        public async Task<ActionResult<MovieDetailsDTO>> Get(int id)
        {
            var movie = await context.Movies
                .Include(x => x.MoviesActors).ThenInclude(x => x.Person)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            return mapper.Map<MovieDetailsDTO>(movie);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme/*, Roles = "Admin"*/)]
        public async Task<ActionResult> Post([FromBody] MovieCreationDTO movieCreationDTO)
        {
            if (movieCreationDTO.Poster == null)
                movieCreationDTO.Poster = "";
            var movie = mapper.Map<Movie>(movieCreationDTO);

            //if (movieCreationDTO.Poster != null)
            //{
            //    using (var memoryStream = new MemoryStream())
            //    {
            //        await movieCreationDTO.Poster.CopyToAsync(memoryStream);
            //        var content = memoryStream.ToArray();
            //        var extension = Path.GetExtension(movieCreationDTO.Poster.FileName);
            //        movie.Poster =
            //            await fileStorageService.SaveFile(content, extension, containerName,
            //                                                movieCreationDTO.Poster.ContentType);
            //    }
            //}

            AnnotateActorsOrder(movie);

            context.Add(movie);
            await context.SaveChangesAsync();
            var movieDTO = mapper.Map<MovieDTO>(movie);
            return new CreatedAtRouteResult("getMovie", new { id = movie.Id }, movieDTO);
        }

        private static void AnnotateActorsOrder(Movie movie)
        {
            if (movie.MoviesActors != null)
            {
                for (int i = 0; i < movie.MoviesActors.Count; i++)
                {
                    movie.MoviesActors[i].Order = i;
                }
            }
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme/*, Roles = "Admin"*/)]
        public async Task<ActionResult> Put(int id, [FromBody] MovieCreationDTO movieCreationDTO)
        {
            var movieDB = await context.Movies.FirstOrDefaultAsync(x => x.Id == id);

            if (movieDB == null)
            {
                return NotFound();
            }

            movieDB = mapper.Map(movieCreationDTO, movieDB);

            //if (movieCreationDTO.Poster != null)
            //{
            //    using (var memoryStream = new MemoryStream())
            //    {
            //        await movieCreationDTO.Poster.CopyToAsync(memoryStream);
            //        var content = memoryStream.ToArray();
            //        var extension = Path.GetExtension(movieCreationDTO.Poster.FileName);
            //        movieDB.Poster =
            //            await fileStorageService.EditFile(content, extension, containerName,
            //                                                movieDB.Poster,
            //                                                movieCreationDTO.Poster.ContentType);
            //    }
            //}

            await context.Database.ExecuteSqlInterpolatedAsync($"delete from MoviesActors where MovieId = {movieDB.Id};");

            AnnotateActorsOrder(movieDB);

            await context.SaveChangesAsync();
            return NoContent();
        }


        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme/*, Roles = "Admin"*/)]
        public async Task<ActionResult> Delete(int id)
        {
            var exists = await context.Movies.AnyAsync(x => x.Id == id);

            if (!exists)
            {
                return NotFound();
            }

            context.Remove(new Movie() { Id = id });
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
