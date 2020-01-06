<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template match="/">
    <xsl:text disable-output-escaping='yes'>&lt;!DOCTYPE HTML></xsl:text>
    <html lang="en-US">
      <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <meta http-equiv="content-type" content="text/html; charset=UTF-8" />
        <title>Movie List: Grizzly Bear Cabin</title>
        <link href="movieposters.css" rel="stylesheet" type="text/css" />
      </head>

      <body class="no-touch">
        <xsl:for-each select="categorylist">
          <table width="100%">
            <tr>
              <td>
                <h1>Movie List</h1>
              </td>
              <td align="right">
                <a href="index.html">List View</a>
              </td>
            </tr>
          </table>
          <h2>Grizzly Bear Cabin</h2>
          <xsl:for-each select="category">
            <xsl:sort select="@ordinal" />
            <xsl:sort select="@name" />
            <div class="wrap">

              <xsl:for-each select="movie">
                <xsl:sort select="@sortname"/>
                <div class="box">
                  <div class="boxInner">
                    <xsl:choose>
                      <xsl:when test="@id">
                        <a href="{@moviesitepath}" target="_blank">
                          <img src="{@fullposterpath}" />
                        </a>
                      </xsl:when>
                      <xsl:otherwise>
                        <xsl:choose>
                          <xsl:when test="@fullposterpath">
                            <img src="{@fullposterpath}" />
                          </xsl:when>
                          <xsl:otherwise>
                            <p>
                              <xsl:value-of select="@name"/>
                            </p>
                          </xsl:otherwise>
                        </xsl:choose>
                      </xsl:otherwise>
                    </xsl:choose>
                    <xsl:choose>
                      <xsl:when test="@originalname">
                        <div class="titleBox">
                          <xsl:value-of select="@name"/>
                          <br />
                          <span style="font: 18px wingdings,webdings;">
                          <xsl:value-of select="@stars"/>
                          </span>
                        </div>
                      </xsl:when>
                    </xsl:choose>
                  </div>
                </div>
              </xsl:for-each>
            </div>
          </xsl:for-each>
        </xsl:for-each>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>