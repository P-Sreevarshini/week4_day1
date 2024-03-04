using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dotnetapp.Models;
using dotnetapp.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace dotnetapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly ReviewService _reviewService;
        private readonly IAuthService _authService; // Inject IAuthService

        public ReviewController(ReviewService reviewService, IAuthService authService)
        {
            _reviewService = reviewService;
            _authService = authService; // Assign injected IAuthService
        }
        
        // [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllReviews()
        {
            try
            {
                var reviews = await _reviewService.GetAllReviewsAsync();
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving reviews: {ex.Message}");
            }
        }

        // [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetReviewsByUserId(long userId)
        {
            try
            {
                var reviews = await _reviewService.GetReviewsByUserIdAsync(userId);
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving reviews for user ID {userId}: {ex.Message}");
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] Review review)
        {
            if (review == null)
            {
                return BadRequest("Review data is null");
            }

            try
            {
                var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated");
                }

                var userId = long.Parse(userIdClaim.Value);
                review.UserId = userId;

                var addedReview = await _reviewService.AddReviewAsync(review);

                // Fetch user details based on the user ID in the review
                var user = await _authService.GetUserByIdAsync(review.UserId);
                if (user == null)
                {
                    // Handle the case where the user is not found (optional)
                    return BadRequest("User not found");
                }

                // Include user ID and user details in the response
                var response = new
                {
                    ReviewId = addedReview.ReviewId,
                    Body = addedReview.Body,
                    Rating = addedReview.Rating,
                    DateCreated = addedReview.DateCreated,
                    UserId = user.UserId, // Add user ID to the response
                    Username = user.Username // Assuming 'Username' is the property that holds the user's name
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while adding a review: {ex.Message}");
            }
        }
    }
}
