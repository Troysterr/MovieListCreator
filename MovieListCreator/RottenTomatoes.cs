using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

namespace MovieListCreator {
  /// <summary>
  /// 
  /// </summary>
  public static class RottenTomatoesApiClient {
    #region fields

    /// <summary>
    /// Base URL for the Weather Endpoint URL
    /// </summary>
    private const string baseUrl = "http://api.rottentomatoes.com/api/public/v1.0/movies.json?q={0}&page_limit=10&page=1&apikey={1}";
    private const string API_KEY = "8ab3g3ak57vx7w6wy6fb9gjb";

    #endregion

    #region methods

    /// <summary>
    /// Searches for a movie, given the movie name 
    /// </summary>
    /// <param name="movieFileName">The name of the movie</param>
    public static RTMovie SearchMovie(string movieName, int movieYear) {
      // Customize URL according to geo location parameters
      var url = string.Format(baseUrl, movieName, API_KEY);
      string content = string.Empty;

      // Syncronious Consumption
      using (var syncClient = new WebClient()) {
        content = syncClient.DownloadString(url);
      }

      // Create the Json serializer and parse the response
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(RootObject));
      using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(content))) {
        // deserialize the JSON object using the WeatherData type.
        var movieData = (RootObject)serializer.ReadObject(ms);

        // If no movie with the correct year is found, just use the first one
        RTMovie tempMovie = movieData.movies[0];

        foreach (RTMovie rtMovie in movieData.movies) {
          if (int.Parse(rtMovie.year) == movieYear) {
            tempMovie = rtMovie;
            break;
          }
        }
        return tempMovie;
      }
    }
    #endregion
  }

  #region JSON serialization classes

  public class ReleaseDates {
    public string theater { get; set; }
    public string dvd { get; set; }
  }

  public class Ratings {
    public string critics_rating { get; set; }
    public int critics_score { get; set; }
    public string audience_rating { get; set; }
    public int audience_score { get; set; }
  }

  public class Posters {
    public string thumbnail { get; set; }
    public string profile { get; set; }
    public string detailed { get; set; }
    public string original { get; set; }
  }

  public class AbridgedCast {
    public string name { get; private set; }
    public string id { get; private set; }
    public List<string> characters { get; private set; }
  }

  public class AlternateIds {
    public string imdb { get; set; }
  }

  public class Links {
    public string self { get; set; }
    public string alternate { get; set; }
    public string cast { get; set; }
    public string clips { get; set; }
    public string reviews { get; set; }
    public string similar { get; set; }
  }

  public class RTMovie {
    public string id { get; set; }
    public string title { get; set; }
    public string year { get; set; }
    public string mpaa_rating { get; set; }
    public string runtime { get; set; }
    public string critics_consensus { get; set; }
    public ReleaseDates release_dates { get; set; }
    public Ratings ratings { get; set; }
    public string synopsis { get; set; }
    public Posters posters { get; set; }
    public List<AbridgedCast> abridged_cast { get; set; }
    public AlternateIds alternate_ids { get; set; }
    public Links links { get; set; }
  }

  public class Links2 {
    public string self { get; set; }
  }

  public class RootObject {
    public int total { get; set; }
    public List<RTMovie> movies { get; set; }
    public Links2 links { get; set; }
    public string link_template { get; set; }
  }

  #endregion
}
