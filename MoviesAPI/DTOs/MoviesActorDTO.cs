using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoviesAPI.DTOs
{
    public class MoviesActorDTO
    {
        public int MovieId { get; set; }
        public string PersonId { get; set; }
        public string Character { get; set; }
        public string Title { get; set; }
        public string Poster { get; set; }


    }
}
