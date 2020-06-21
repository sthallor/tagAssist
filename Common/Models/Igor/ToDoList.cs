using System.ComponentModel.DataAnnotations;

namespace Common.Models.Igor
{
    public class ToDoList
    {
        [Key]
        public int Id { get; set; }
        public string Rig { get; set; }
        public string Server { get; set; }
        public string Message { get; set; }
    }
}