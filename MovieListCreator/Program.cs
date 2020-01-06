using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Xsl;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace MovieListCreator {
  /// <summary>
  /// 
  /// </summary>
  class Program {
    /// <summary>
    /// 
    /// </summary>
    struct Movie {
      internal string Name;
      internal string OriginalName;
      internal int Year;
    }

    const string HTML_FOLDER = @"movielist\";
    const string LIST_FILE_NAME = "index.html";
    const string POSTER_FILE_NAME = "posters.html";
    const string SYNOPSIS_FILE_NAME = "synopsis.html";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args) {
      string movieRootPath = Properties.Settings.Default.RootPath;

      foreach (string arg in args) {
        string[] argPair = arg.Split('=');

        switch (argPair[0]) {
          case "path":
            movieRootPath = argPair[1];
            break;
          default:
            Console.WriteLine(Resources.invalidParameter + argPair[0]);
            break;
        }
      }

      // Set output paths
      string listFileName = HTML_FOLDER + LIST_FILE_NAME;
      string posterFileName = HTML_FOLDER + POSTER_FILE_NAME;
      string synopsisFileName = HTML_FOLDER + SYNOPSIS_FILE_NAME;
      listFileName = Path.GetFullPath(listFileName);
      posterFileName = Path.GetFullPath(posterFileName);

      // Initialize the movie list XML
      XmlDocument movieListXml = InitializeMovieList();

      // Update the movie list XML with any new information
      foreach (string sDir in movieRootPath.Split('+')) {
        string sDirNow = Path.GetFullPath(sDir);
        readFolder(sDirNow, movieListXml);
      }

      // Remove any template category elements with 0 movies...
      foreach (XmlNode ndEmpty in movieListXml.SelectNodes("//category[@count=\"0\"]")) {
        ndEmpty.ParentNode.RemoveChild(ndEmpty);
      }
      // Remove any "untouched" elements
      foreach (XmlNode ndMovie in movieListXml.SelectNodes("//movie[not (@touched)]")) {
        ndMovie.ParentNode.RemoveChild(ndMovie);
      }
      // Removed "touched" attributes
      foreach (XmlNode ndMovie in movieListXml.SelectNodes("//movie")) {
        ((XmlElement)ndMovie).RemoveAttribute("touched");
      }

      // Process images
      ImageHelper.cachePosterImages(movieListXml, HTML_FOLDER);
      ImageHelper.createCategoryImages(movieListXml, HTML_FOLDER);

      // Write out the updated movie list XML
      File.WriteAllText("movielist.xml", movieListXml.OuterXml);

      // Write out the new HTML pages
      TransformMovieList("xml.movielist", listFileName);
      TransformMovieList("xml.movieposters", posterFileName);
      TransformMovieList("xml.synopsislist", synopsisFileName);
    }

    /// <summary>
    /// Load up the existing movie list XML, or create a new from the temlate if one
    /// doesn't already exist.
    /// </summary>
    /// <returns>A reference to an XmlDocument</returns>
    private static XmlDocument InitializeMovieList() {
      XmlDocument movieListXml = new XmlDocument();
      if (File.Exists("movielist.xml")) {
        movieListXml.Load("movielist.xml");

        movieListXml.DocumentElement.SetAttribute("count", "0");

        foreach (XmlNode ndCategory in movieListXml.SelectNodes("//category")) {
          ((XmlElement)ndCategory).SetAttribute("count", "0");
        }
      }
      else {
        movieListXml.LoadXml(Resources.template); // "template.xml");
      }
      return movieListXml;
    }

    /// <summary>
    /// Given the name of a XSLT resource and an output path, transforms the movie list XML
    /// into an HTML document.
    /// </summary>
    /// <param name="xsltFileName">The name of the XSLT resource file</param>
    /// <param name="movieListXml">The path to the file to be written</param>
    private static void TransformMovieList(string xsltFileName, string movieListXml) {
      Stream strm = Assembly.GetExecutingAssembly().GetManifestResourceStream(string.Format("MovieListCreator.{0}.xslt", xsltFileName));
      using (XmlReader reader = XmlReader.Create(strm)) {
        XslCompiledTransform transform = new XslCompiledTransform();
        transform.Load(reader);
        transform.Transform("movielist.xml", movieListXml);

      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="movieListXml"></param>
    static void readFolder(string folderPath, XmlDocument movieListXml) {
      DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
      const int MIN_FILE_SIZE = 100 * 1000 * 1000; // Don't bother wtih files smaller than 100 MB

      // Process subfolders recursively
      foreach (string sSubFolder in Directory.GetDirectories(folderPath)) {
        const string PLEX_VERSIONS = "Plex Versions";
        if (!sSubFolder.EndsWith(PLEX_VERSIONS)) {
          readFolder(sSubFolder, movieListXml);
        }
      }

      // Iterate through all of the files in the folder
      foreach (string sFile in Directory.GetFiles(folderPath)) {
        if (!sFile.ToLower().EndsWith(".srt")) {
          Console.WriteLine("Processing " + sFile);

          // Make sure the file is at least MIN_FILE_SIZE bytes in size
          // (otherwise, it's probably not a movie)
          FileInfo fileInfo = new FileInfo(sFile);
          if (fileInfo.Length > MIN_FILE_SIZE && !fileInfo.Name.StartsWith("~")) {
            // Get the category element for the movie
            XmlElement elCategory = GetMovieCategory(movieListXml, dirInfo);

            // Get the movie details (name and year) from the filename
            string movieFileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length).TrimEnd();
            Movie movie = getMovie(movieFileName);

            // Check to see if we already have a node with the same "original name"
            XmlElement elMovie = (XmlElement)elCategory.SelectSingleNode("movie[@originalname=\"" + movie.Name + "\"]");
            // If it doesn't already exist in the XML document...
            if (elMovie == null) {
              //...then search TMDB for it
              elMovie = SearchTmdbForMovie(movieListXml, elCategory, ref movie);
            }
            elMovie.SetAttribute("touched", "1");

            // If the movie doesn't have an audience score (from Rotten Tomatoes)...
            if (string.IsNullOrEmpty(elMovie.GetAttribute("audiencescore"))) {
              //...then go get it
              GetRottenTomatoesInfo(movieListXml, movie, elMovie);
            }

            // Increment the count of movies in the category
            int iCategoryCount = int.Parse(elCategory.GetAttribute("count")) + 1;
            elCategory.SetAttribute("count", iCategoryCount.ToString());
            // Increment the total count of movies
            iCategoryCount = int.Parse(movieListXml.DocumentElement.GetAttribute("count")) + 1;
            movieListXml.DocumentElement.SetAttribute("count", iCategoryCount.ToString());
          }
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="movieListXml"></param>
    /// <param name="elCategory"></param>
    /// <param name="movie"></param>
    /// <returns></returns>
    private static XmlElement SearchTmdbForMovie(XmlDocument movieListXml, XmlElement elCategory, ref Movie movie) {
      TMDbClient client = new TMDbClient("9c09fcd45f010d680dff06c2af007a04");
      SearchContainer<SearchMovie> results = null;
      SearchMovie result = null;

      // Kludge for special case
      string movieName = movie.Name;
      if (movieName == "X-Men Apocalypse") {
        movieName = "X-Men: Apocalypse";
      }

      int attemptCount = 3;
      if (movie.Year != 0) {
        do {
          try
          {
            results = client.SearchMovieAsync(movieName, includeAdult: false, year: movie.Year).Result;
          }
          catch { }

          if (results == null || results.Results == null) {
            Thread.Sleep(3000);
            results = null;
          }
          attemptCount--;
        } while (attemptCount > 0 && results == null);
      }

      if (movie.Year == 0 || results == null) {
        results = client.SearchMovieAsync(movieName, includeAdult: false).Result;
      }

      // If any results were returned...
      if (results != null && results.Results != null && results.Results.Count > 0) {
        //...default to the first result
        result = results.Results[0];

        // If there is more than one result...
        if (results.Results.Count > 1) {
          //...find the first one that matches the movie name exactly
          foreach (SearchMovie searchMovie in results.Results) {
            if (searchMovie.Title == movieName) {
              result = searchMovie;
              break;
            }
          }
        }
        movie.Name = result.Title;
        movie.Year = int.Parse(result.ReleaseDate.Value.Year.ToString());
      }

      // Now that we have the "official" name of the movie, check to see if we have a matching movie
      return GetMovieElement(movieListXml, elCategory, ref movie, result);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="movieListXml"></param>
    /// <param name="dirInfo"></param>
    /// <returns></returns>
    private static XmlElement GetMovieCategory(XmlDocument movieListXml, DirectoryInfo dirInfo) {
      XmlElement elCategory = (XmlElement)movieListXml.DocumentElement.SelectSingleNode(string.Format("category[@alias='{0}' or @name='{0}']", dirInfo.Name));
      if (elCategory == null) {
        elCategory = (XmlElement)(movieListXml.DocumentElement.AppendChild(movieListXml.CreateElement("category")));
        elCategory.SetAttribute("name", dirInfo.Name);
        elCategory.SetAttribute("count", "0");
      }
      return elCategory;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="movieListXml"></param>
    /// <param name="elCategory"></param>
    /// <param name="movie"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private static XmlElement GetMovieElement(XmlDocument movieListXml, XmlElement elCategory, ref Movie movie, SearchMovie result) {
      XmlElement elMovie = (XmlElement)elCategory.SelectSingleNode("movie[@name=\"" + movie.Name + "\"]");
      if (elMovie == null) {
        elMovie = (XmlElement)(elCategory.AppendChild(movieListXml.CreateElement("movie")));
        elMovie.SetAttribute("name", movie.Name);
        elMovie.SetAttribute("originalname", movie.OriginalName);
        elMovie.SetAttribute("year", movie.Year.ToString());

        if (result != null) {
          elMovie.SetAttribute("id", result.Id.ToString());
          elMovie.SetAttribute("moviesitepath", @"http://www.themoviedb.org/movie/" + result.Id.ToString());
          elMovie.SetAttribute("posterpath", result.PosterPath);
          elMovie.SetAttribute("backdroppath", result.BackdropPath);
          elMovie.SetAttribute("popularity", result.Popularity.ToString());
          elMovie.SetAttribute("voteaverage", result.VoteAverage.ToString());
          elMovie.SetAttribute("votecount", result.VoteCount.ToString());

          XmlNode elChild = elMovie.AppendChild(elMovie.OwnerDocument.CreateElement("consensus"));
          string plot = FormatPlot(result.Overview);
          elChild.InnerText = plot;
        }

        // Remove leading "The ", if there is one
        string sSortName = elMovie.GetAttribute("name");
        if (sSortName.Length > 4 && sSortName.ToUpper().StartsWith("THE ")) {
          sSortName = sSortName.Substring(4);
        }

        elMovie.SetAttribute("sortname", sSortName);
      }
      return elMovie;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="movieListXml"></param>
    /// <param name="movie"></param>
    /// <param name="elMovie"></param>
    private static void GetRottenTomatoesInfo(XmlDocument movieListXml, Movie movie, XmlElement elMovie) {
      try {
        GetMoviePlot(elMovie, movie);
      }
      catch (Exception e) {
        elMovie.SetAttribute("rtError", e.Message);
      } // If this call fails, don't worry...catch it next time
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="imdbId"></param>
    /// <param name="movieFileName"></param>
    /// <param name="movieYear"></param>
    /// <returns></returns>
    private static void GetMoviePlot(XmlElement elMovie, Movie movie)
    {
      const string SEARCH_BY_NAME = "http://www.omdbapi.com/?t={0}&y={1}&plot=short&r=xml&apikey=1349d8a2";
      XmlDocument omdbXml = new XmlDocument();

      GetUriResults(omdbXml, new Uri(string.Format(SEARCH_BY_NAME, WebUtility.UrlEncode(movie.OriginalName), movie.Year)));

      XmlElement movieNode = (XmlElement)omdbXml.DocumentElement.SelectSingleNode("movie");

      string imdbId = movieNode.GetAttribute("imdbID");
      string plot = WebUtility.HtmlDecode(movieNode.GetAttribute("plot"));
      plot = FormatPlot(plot);

      double imdbRating = 0;
      if (double.TryParse(movieNode.GetAttribute("imdbRating"), out imdbRating))
      {
        elMovie.SetAttribute("audiencescore", movieNode.GetAttribute("imdbRating"));
        // Assign # of stars based on score
        string sStars = new string((char)171,
            imdbRating > 8.5 ? 5
          : imdbRating > 7.0 ? 4
          : imdbRating > 5.5 ? 3
          : imdbRating > 3.0 ? 2
          : 1);

        elMovie.SetAttribute("stars", sStars);
      }

      XmlNode elChild = elMovie.SelectSingleNode("consensus");
      if (elChild == null)
      {
        elChild = elMovie.AppendChild(elMovie.OwnerDocument.CreateElement("consensus"));
      }
      elChild.InnerXml = plot;

      elMovie.SetAttribute("moviesitepath", "http://www.imdb.com/title/" + imdbId);
    }

    private static string FormatPlot(string plot)
    {
      const int MAX_PLOT_LENGTH = 300;

      if (plot.Length > MAX_PLOT_LENGTH)
      {
        int pos = plot.LastIndexOf(' ', MAX_PLOT_LENGTH);
        const string TRUNCATE_STUFF = "{0}&lt;span title=\"{1}\" &gt;[...]&lt;/span&gt;";
        plot = string.Format(TRUNCATE_STUFF, plot.Substring(0, pos), plot.Substring(pos));
      }

      return plot;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="omdbXml"></param>
    /// <param name="serviceUri"></param>
    private static void GetUriResults(XmlDocument omdbXml, Uri serviceUri) {
      using (var httpClient = new HttpClient()) {
        using (HttpResponseMessage response = httpClient.GetAsync(serviceUri).Result) {
          omdbXml.LoadXml(response.Content.ReadAsStringAsync().Result);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="movieFileName"></param>
    /// <returns></returns>
    private static Movie getMovie(string movieFileName) {
      Movie movie = new Movie();

      Regex regex = new Regex(@" Disc \d");
      movieFileName = regex.Replace(movieFileName, "");

      regex = new Regex(@" - Color");
      movieFileName = regex.Replace(movieFileName, "");

      regex = new Regex(@"\((\d{4})\)$");
      Match match = regex.Match(movieFileName);
      if (match.Success) {
        movie.Name = movieFileName.Substring(0, movieFileName.Length - 7);
        movie.OriginalName = movie.Name;
        movie.Year = int.Parse(match.Groups[1].Value);
      }
      else {
        movie.Name = movieFileName;
        movie.Year = 0;
      }

      return movie;
    }
  }
}
