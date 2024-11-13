using Microsoft.AspNetCore.Mvc;
using QuizApplication.Data;
using QuizApplication.Models;
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
            else
            {

            }
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var userQuiz = new UserQuiz
        {
            UserId = userId,
            QuizId = quizId,
            IsCompleted = true,
            Score = score
        };
        _context.UserQuizzes.Add(userQuiz);
        await _context.SaveChangesAsync();

        ViewBag.Score = score;
        return View("Result", score);
    }

    public IActionResult Leaderboard(int quizId)
    {
        var leaderboard = _context.UserQuizzes
                .Where(uq => uq.QuizId == quizId && uq.IsCompleted)
                .GroupBy(uq => uq.UserId)
                .Select(group => new
                {
                    UserId = group.Key,
                    BestScore = group.Max(uq => uq.Score),
                    QuizId = group.FirstOrDefault().QuizId
                })
                .Join(_context.Users,
                    result => result.UserId,
                    user => user.Id,
                    (result, user) => new { result, user })
                .Join(_context.Quizzes,
                    result => result.result.QuizId,
                    quiz => quiz.Id,
                    (result, quiz) => new LeaderboardEntryViewModel
                    {
                        UserName = result.user.UserName ?? "Unknown",
                        QuizTitle = quiz.Title ?? "No Title",
                        Score = result.result.BestScore
                    })
                .OrderByDescending(entry => entry.Score)
                .ToList();
        return View(leaderboard);
    }
}
