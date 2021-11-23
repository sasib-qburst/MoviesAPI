using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoviesAPI.DTOs
{
    public class MovieDetailsDTO: MovieDTO
    {
        public List<ActorDTO> Actors { get; set; }
    }
}
