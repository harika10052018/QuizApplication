using Microsoft.AspNetCore.Mvc;
using System.Linq;
using QuizApplication.Data;
using QuizApplication.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[Authorize]
public class QuizController : Controller
{
    private readonly ApplicationDbContext _context;

    public QuizController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Leaderboard(int quizId)
    {
        var leaderboard = _context.UserQuizzes
                .Where(uq => uq.QuizId == quizId && uq.IsCompleted)
                .GroupBy(uq => uq.UserId) // Group by UserId to find the best score for each user
                .Select(group => new
                {
                    UserId = group.Key,
                    BestScore = group.Max(uq => uq.Score), // Get the best score for each user
                    QuizId = group.FirstOrDefault().QuizId // Get the QuizId for the associated quiz
                })
                .Join(_context.Users, // Join with Users table to get User details
                    result => result.UserId,
                    user => user.Id,
                    (result, user) => new { result, user })
                .Join(_context.Quizzes, // Join with Quizzes table to get Quiz details
                    result => result.result.QuizId,
                    quiz => quiz.Id,
                    (result, quiz) => new LeaderboardEntryViewModel
                    {
                        UserName = result.user.UserName ?? "Unknown",
                        QuizTitle = quiz.Title ?? "No Title",
                        Score = result.result.BestScore
                    })
                .OrderByDescending(entry => entry.Score) // Order the leaderboard by score
                .ToList();
        return View(leaderboard);
    }

    // Action to display a list of quizzes
    public async Task<IActionResult> Index()
    {
        var quizzes = await _context.Quizzes.ToListAsync();
        return View(quizzes);
    }

    public async Task<IActionResult> Index1()
    {
        var quizzes = await _context.Quizzes.ToListAsync();
        return View(quizzes);
    }

    // Action to display quiz questions
    public async Task<IActionResult> StartQuiz(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
        {
            return NotFound();
        }

        return View(quiz);
    }

    // Action to submit quiz answers
    [HttpPost]
    public async Task<IActionResult> SubmitQuiz(int quizId, Dictionary<int, int> answers)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null)
        {
            return NotFound();
        }

        int score = 0;
        foreach (var question in quiz.Questions)
        {
            var selectedAnswerId = answers[question.Id];
            var selectedAnswer = question.Answers.FirstOrDefault(a => a.Id == selectedAnswerId);
            if (selectedAnswer != null && selectedAnswer.IsCorrect)
            {
                score++;
            }
        }

        // Get the correct UserId
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Ensure the UserId is valid
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(); // Handle the case where user is not authenticated
        }

        // Save score to UserQuiz table
        var userQuiz = new UserQuiz
        {
            UserId = userId, // Assuming you store user info in Identity
            QuizId = quizId,
            IsCompleted = true,
            Score = score
        };
        _context.UserQuizzes.Add(userQuiz);
        await _context.SaveChangesAsync();

        ViewBag.Score = score;
        return View("Result", score); // Assuming you have a Result view to display the score
    }
}
