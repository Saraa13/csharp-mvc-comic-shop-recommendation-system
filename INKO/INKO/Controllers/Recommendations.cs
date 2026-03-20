using INKO.Models;
using INKO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public class RecommendationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public RecommendationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private List<int> excludedProducts = new List<int>(); // Lista već preporučenih manga

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"ClientId: {userId}");
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("Invalid user.");
        }

       

        // Kombinujemo sve preporuke sa tipovima
        var allRecommendations = new List<dynamic>();

        var genreRecommendations = await GetGenreBasedRecommendations(userId);
        if (genreRecommendations.Any())
        {
            allRecommendations.AddRange(genreRecommendations.Select(p => new { Product = p, RecommendationType = "Based on your favorite genre" }));
        }

        var sequentialRecommendation = await GetSequentialPurchaseRecommendations(userId);
        if (sequentialRecommendation.Any())
        {
            allRecommendations.AddRange(sequentialRecommendation.Select(p => new { Product = p, RecommendationType = "Based on your sequential purchases" }));
        }

        var publisherRecommendations = await GetPublisherBasedRecommendation(userId);
        if (publisherRecommendations.Any())
        {
            allRecommendations.AddRange(publisherRecommendations.Select(p => new { Product = p, RecommendationType = "Based on the same publisher" }));
        }

        var mostPopular = await GetMostPopularBooks(userId);
        if (mostPopular.Any())
        {
            allRecommendations.AddRange(mostPopular.Select(p => new { Product = p, RecommendationType = "Most popular books" }));
        }

        return View("Recommendations", allRecommendations);





    }





    //PREPORUKE
private async Task<List<Product>> GetGenreBasedRecommendations(string clientId)
    {
        var query = @"
        WITH FavoriteGenres AS (
            SELECT TOP 1 LTRIM(RTRIM(value)) AS Genre
            FROM Orders o
            JOIN OrderItems oi ON o.Id = oi.OrderId
            JOIN Products p ON oi.ProductId = p.Id
            CROSS APPLY STRING_SPLIT(p.Genre, ',')  
            WHERE o.ClientId = @clientId
            GROUP BY LTRIM(RTRIM(value))  
            HAVING COUNT(*) >= 2
            ORDER BY COUNT(*) DESC
        )
        SELECT TOP 1 p.*
        FROM Products p
        WHERE EXISTS (
            SELECT 1 FROM FavoriteGenres fg WHERE p.Genre LIKE '%' + fg.Genre + '%'
        ) 
        AND p.Id NOT IN (
            SELECT oi.ProductId FROM Orders o
            JOIN OrderItems oi ON o.Id = oi.OrderId
            WHERE o.ClientId = @clientId
        )
        AND p.Id NOT IN (@excludedProducts)
        ORDER BY p.CreatedAt DESC;";

        var clientIdParam = new SqlParameter("@clientId", clientId);
        var excludedParam = new SqlParameter("@excludedProducts", string.Join(",", excludedProducts));

        var result = await _context.Products.FromSqlRaw(query, clientIdParam, excludedParam).ToListAsync();
        excludedProducts.AddRange(result.Select(p => p.Id)); // Dodaj u isključene preporuke
        return result;
    }

    public async Task<List<Product>> GetSequentialPurchaseRecommendations(string clientId)
    {
        var query = @"
        WITH LastPurchase AS (
            SELECT TOP 1 oi.ProductId AS LastManga
            FROM Orders o
            JOIN OrderItems oi ON o.Id = oi.OrderId
            WHERE o.ClientId = @clientId
            ORDER BY o.Id DESC
        ),
        NextPurchases AS (
            SELECT oi.ProductId AS NextManga, COUNT(*) AS PurchaseCount
            FROM Orders o
            JOIN OrderItems oi ON o.Id = oi.OrderId
            WHERE oi.ProductId <> (SELECT LastManga FROM LastPurchase)
            AND o.ClientId <> @clientId
            AND EXISTS (
                SELECT 1 FROM OrderItems oi2
                JOIN Orders o2 ON o2.Id = oi2.OrderId
                WHERE o2.ClientId <> @clientId 
                AND oi2.ProductId = (SELECT LastManga FROM LastPurchase)
            )
            GROUP BY oi.ProductId
        )
        SELECT TOP 1 p.*
        FROM Products p
        JOIN NextPurchases np ON p.Id = np.NextManga
        WHERE p.Id NOT IN (
            SELECT oi.ProductId 
            FROM Orders o
            JOIN OrderItems oi ON o.Id = oi.OrderId
            WHERE o.ClientId = @clientId
        )
        AND p.Id NOT IN (@excludedProducts)
        ORDER BY np.PurchaseCount DESC;";

        var clientParam = new SqlParameter("@clientId", clientId);
        var excludedParam = new SqlParameter("@excludedProducts", string.Join(",", excludedProducts));

        var result = await _context.Products.FromSqlRaw(query, clientParam, excludedParam).ToListAsync();
        excludedProducts.AddRange(result.Select(p => p.Id)); // Dodaj u isključene preporuke
        return result;
    }

    public async Task<List<Product>> GetPublisherBasedRecommendation(string clientId)
    {
        string excludedIds = excludedProducts.Any() ? string.Join(",", excludedProducts) : "NULL";

        var query = $@"
        WITH LastPurchase AS (
            SELECT TOP 1 p.Publisher
            FROM Orders o
            JOIN OrderItems oi ON o.Id = oi.OrderId
            JOIN Products p ON oi.ProductId = p.Id
            WHERE o.ClientId = @clientId
            ORDER BY o.Id DESC
        )
        SELECT TOP 1 p.*
        FROM Products p
        WHERE p.Publisher = (SELECT Publisher FROM LastPurchase)
        AND p.Id NOT IN (
            SELECT oi.ProductId FROM Orders o
            JOIN OrderItems oi ON o.Id = oi.OrderId
            WHERE o.ClientId = @clientId
        )
        AND p.Id NOT IN ({excludedIds}) 
        ORDER BY NEWID();";

        var clientParam = new SqlParameter("@clientId", clientId);

        var result = await _context.Products.FromSqlRaw(query, clientParam).ToListAsync();
        excludedProducts.AddRange(result.Select(p => p.Id)); // Dodaj u isključene preporuke
        return result;
    }


    public async Task<List<Product>> GetMostPopularBooks(string clientId)
    {
        string excludedIds = excludedProducts.Any() ? string.Join(",", excludedProducts) : "NULL";

        var query = $@"
    WITH UserPurchasedBooks AS (
        SELECT oi.ProductId 
        FROM Orders o
        JOIN OrderItems oi ON o.Id = oi.OrderId
        WHERE o.ClientId = @clientId
    ),
    PopularBooks AS (
        SELECT oi.ProductId, COUNT(DISTINCT o.ClientId) AS UniqueBuyers
        FROM Orders o
        JOIN OrderItems oi ON o.Id = oi.OrderId
        WHERE oi.ProductId NOT IN (SELECT ProductId FROM UserPurchasedBooks)  -- Isključujemo već kupljene knjige
        GROUP BY oi.ProductId
    )
    SELECT TOP 1 p.*
    FROM Products p
    JOIN PopularBooks pb ON p.Id = pb.ProductId
    WHERE p.Id NOT IN ({excludedIds})  -- Izuzimamo već preporučene knjige
    ORDER BY pb.UniqueBuyers DESC;";

        var result = await _context.Products.FromSqlRaw(query, new SqlParameter("@clientId", clientId)).ToListAsync();

        excludedProducts.AddRange(result.Select(p => p.Id));

        return result;
    }






}