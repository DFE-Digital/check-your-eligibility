using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckYourEligibility.Data.Models
{
    public interface IStudentCommand
    {
        Task<IEnumerable<Student>> GetStudents();
    }

    public class StudentCommand : IStudentCommand
    {
        private readonly IEligibilityCheckContext _db;
        public StudentCommand(IEligibilityCheckContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Student>> GetStudents()
        {
            var result = await _db.Students.ToListAsync();
            return result;
        }
    }
}
