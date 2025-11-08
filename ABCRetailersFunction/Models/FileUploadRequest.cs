using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ABCRetailersFunction.Models
{
    public class FileUploadRequest
    {
        public IFormFile? File { get; set; }
        public string? ContainerName { get; set; }
        public string? ShareName { get; set; }
        public string? DirectoryName { get; set; }
    }
}
