using Microsoft.AspNetCore.Identity;

namespace QuizApplication.Models
{
    public class UserQuiz
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int QuizId { get; set; }
        public bool IsCompleted { get; set; }
        public int Score { get; set; }

        public IdentityUser User { get; set; }
        public Quiz Quiz { get; set; }
    }
}
