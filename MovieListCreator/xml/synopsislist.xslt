<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template match="/">
    <xsl:text disable-output-escaping='yes'>&lt;!DOCTYPE HTML></xsl:text>
    <html lang="en-US">
      <head>
        <meta charset="utf-8" />
        <meta http-equiv="X-UA-Compatible" content="IE=edge" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <meta name="description" content="" />
        <meta name="author" content="" />

        <title>Grizzly Bear Cabin Movie List</title>

        <!-- Bootstrap core CSS -->
        <link href="Content/bootstrap.min.css" rel="stylesheet" />

        <!-- HTML5 shim and Respond.js IE8 support of HTML5 elements and media queries -->
        <!--[if lt IE 9]>
      <script src="https://oss.maxcdn.com/libs/html5shiv/3.7.0/html5shiv.js"></script>
      <script src="https://oss.maxcdn.com/libs/respond.js/1.4.2/respond.min.js"></script>
    <![endif]-->
  <script language="javascript">
    function untruncate(id) {
      document.getElementById('hidden' + id).outerHTML = document.getElementById('hidden' + id).innerText;
      document.getElementById('showit' + id).outerHTML = "";
    }
  </script>
      </head>

      <body>
        <xsl:for-each select="categorylist">
          <div class="jumbotron">
            <div class="container">
              <h1>Grizzly Bear Cabin Movie List</h1>
              <p>This is a list of all of the high-definition movies that are available on the media server in the theater room at Grizzly Bear Cabin.</p>
              <p>(Note that there are additional movies available on Netflix and Amazon Prime Movies, as well as a collection of movies recorded on the DirecTV DVR.)</p>
            </div>
          </div>
          <div class="container">
            <xsl:for-each select="category">
              <xsl:sort select="@ordinal" />
              <xsl:sort select="@name" />
              <h3>
                <xsl:value-of select="@name"/>
              </h3>
              <!-- Example row of columns -->
              <div class="row">
                <xsl:for-each select="movie[@name!='']">
                  <xsl:sort select="@sortname"/>
                  <div class="col-md-6">
                    <table>
                      <tr valign="top">
                        <td style="padding-bottom: 28px; padding-right: 12px;">
                          <xsl:choose>
                            <xsl:when test="@id">
                              <a href="{@moviesitepath}" target="_blank">
                                <img src="{@fullposterpath}" style="width: 106px; height: 159px;" />
                              </a>
                            </xsl:when>
                            <xsl:otherwise>
                              <xsl:choose>
                                <xsl:when test="@fullposterpath">
                                  <img src="{@fullposterpath}" style="width: 106px; height: 159px;" />
                                </xsl:when>
                                <xsl:otherwise>
                                  <p>
                                    <xsl:value-of select="@name"/>
                                  </p>
                                </xsl:otherwise>
                              </xsl:choose>
                            </xsl:otherwise>
                          </xsl:choose>
                        </td>
                        <td>
                          <h4>
                            <xsl:value-of select="@name"/> (<xsl:value-of select="@year"/>) 
                          <span style="font: 18px wingdings,webdings;">
                          <xsl:value-of select="@stars"/>
                          </span>
                          </h4>
                          <p>
                            <xsl:value-of select="consensus" disable-output-escaping="yes" />
                          </p>
                          <p>
                            <a class="btn btn-default" href="{@moviesitepath}" target="_blank" role="button">View details</a>
                          </p>
                        </td>
                      </tr>
                    </table>
                  </div>
                </xsl:for-each>
              </div>
            </xsl:for-each>
          </div>
        </xsl:for-each>
        <!-- /container -->

        <!-- Bootstrap core JavaScript
    ================================================== -->
        <!-- Placed at the end of the document so the pages load faster -->
        <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.0/jquery.min.js"></script>
        <script src="Scripts/bootstrap.min.js"></script>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>