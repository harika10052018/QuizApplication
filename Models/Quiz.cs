namespace QuizApplication.Models
{
    public class Quiz
    {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Topic { get; set; }
            public ICollection<Question> Questions { get; set; }
    }
}
