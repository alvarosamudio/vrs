﻿// Copyright © 2013 onwards, Andrew Whewell
// All rights reserved.
//
// Redistribution and use of this software in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of the author nor the names of the program's contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OF THE SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualRadar.Interface.WebSite
{
    /// <summary>
    /// A class that can read a checksum file.
    /// </summary>
    /// <remarks><para>
    /// A checksum file is a flat text file with one line per file. The format of the line is:<br/>
    /// CHECKSUM FILE-SIZE FILE-PATH-FROM-ROOT.
    /// </para><para>
    /// The checksum and file size can be generated by static methods on CheckSumFileEntry.
    /// </para></remarks>
    public static class ChecksumFile
    {
        /// <summary>
        /// Parses the content of a checksum file into checksum file entries.
        /// </summary>
        /// <param name="checksumFileContent"></param>
        /// <param name="enforceContentChecksum">True if the checksum file entries should start with a content checksum</param>
        /// <returns></returns>
        public static List<ChecksumFileEntry> Load(string checksumFileContent, bool enforceContentChecksum)
        {
            var result = new List<ChecksumFileEntry>();

            var lines = checksumFileContent.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if(enforceContentChecksum) {
                EnforceContentChecksum(lines);
            }

            foreach(var line in lines) {
                var entry = CreateFromLine(line);
                if(entry != null) {
                    result.Add(entry);
                }
            }

            return result;
        }

        /// <summary>
        /// Tests the content checksum for validity, throws an exception if it is invalid.
        /// </summary>
        /// <param name="lines"></param>
        private static void EnforceContentChecksum(List<string> lines)
        {
            if(lines.Count == 0) {
                throw new InvalidOperationException("The checksums file is empty");
            }

            var contentLine = lines[0];
            var contentEntry = CreateFromLine(contentLine);
            if(contentEntry == null || contentEntry.FileName != "\\**CONTENT CHECKSUM**") {
                throw new InvalidOperationException($@"""{contentLine}"" is not a valid content checksum line");
            }
            lines.RemoveAt(0);

            var checksumBytes = Encoding.UTF8.GetBytes(String.Concat(lines.ToArray()));     // IF YOU ARE PORTING FROM V3 NOTE THAT .NET 3.5 String.Concat NEEDS TO BE PASSED AN ARRAY
            var checksum = ChecksumFileEntry.GenerateChecksum(checksumBytes);
            var checksumSize = lines.Sum(r => r.Length);

            if(contentEntry.FileSize != checksumSize) {
                throw new InvalidOperationException($"The lines in the checksums file have been altered - was expecting {contentEntry.FileSize} bytes, the lines added up to {checksumSize}");
            }
            if(contentEntry.Checksum != checksum) {
                throw new InvalidOperationException($"The checksums file has been altered - was expecting a checksum of {contentEntry.Checksum}, it was actually {checksum}");
            }
        }

        private static ChecksumFileEntry CreateFromLine(string line)
        {
            ChecksumFileEntry result = null;

            var slashPosn = line.IndexOf('\\');
            if(slashPosn != -1) {
                var chunks = line.Substring(0, slashPosn).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if(chunks.Length == 2) {
                    result = new ChecksumFileEntry() {
                        Checksum = chunks[0],
                        FileSize = long.Parse(chunks[1]),
                        FileName = line.Substring(slashPosn)
                    };
                }
            }

            return result;
        }
    }
}
