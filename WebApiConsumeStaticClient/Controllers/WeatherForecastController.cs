using Microsoft.AspNetCore.Mvc;

namespace WebApiBrowserLog.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;


        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;

        }

        [HttpGet("/GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            //ģ�ⲻͬ����Աͨ��token
            _logger.LogInformation("[token:789]��¼��Ϣ");
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
        /// <summary>
        /// ��ӷ��������쳣��Ϣ
        /// </summary>
        /// <returns></returns>
        [HttpGet("/TestError")]
        public IActionResult TestError()
        { 
           
           
            string result = string.Empty;
            try
            {
                //����Summaries��ֻ��ʮ��Ԫ�أ���������߽磬����
                result = Summaries[20];
            }
            catch (Exception ex)
            {
                //ģ�ⲻͬ����Աͨ��token
                _logger.LogError(ex, "[token:456]"+ex.Message);
            }
            return Ok(result);
        }
    }
}