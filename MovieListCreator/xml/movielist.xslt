<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template match="/">
    <html>
      <head>
        <link href="movielist.css" rel="stylesheet" type="text/css" />
      </head>
      <body>
        <table width="100%">
          <tr>
            <td>
              <h1>Movie List</h1>
            </td>
            <td align="right">
              <a href="posters.html">Poster View</a>
            </td>
          </tr>
        </table>
        <h2>Grizzly Bear Cabin</h2>
        <xsl:for-each select="categorylist">
          <xsl:for-each select="category">
            <xsl:sort select="@ordinal" />
            <xsl:sort select="@name" />
            <h3>
              <xsl:value-of select="@name"/>
              <span style="font-weight: normal;">
                (<xsl:value-of select="@count"/> movies)
              </span>
            </h3>
            <ul>
              <xsl:for-each select="movie[@name!='']">
                <xsl:sort select="@sortname"/>
                <li>
                  <xsl:choose>
                    <xsl:when test="@id">
                      <a href="{@moviesitepath}">
                        <xsl:value-of select="@name"/>
                      </a>
                    </xsl:when>
                    <xsl:otherwise>
                      <xsl:value-of select="@name"/>
                    </xsl:otherwise>
                  </xsl:choose> (<xsl:value-of select="@year"/>)
                </li>
              </xsl:for-each>
            </ul>
          </xsl:for-each>
          <h3>
            Total movies: <xsl:value-of select="@count"/>
          </h3>
        </xsl:for-each>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
