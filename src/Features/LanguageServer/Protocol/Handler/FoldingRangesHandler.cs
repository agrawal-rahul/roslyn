﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class FoldingRangesHandler
    {
        internal static async Task<FoldingRange[]> GetFoldingRangeAsync(Solution solution, FoldingRangeParams request, CancellationToken cancellationToken)
        {
            var foldingRanges = ArrayBuilder<FoldingRange>.GetInstance();

            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document == null)
            {
                return foldingRanges.ToArrayAndFree();
            }

            var blockStructureService = document.Project.LanguageServices.GetService<BlockStructureService>();
            if (blockStructureService == null)
            {
                return foldingRanges.ToArrayAndFree();
            }

            var blockStructure = await blockStructureService.GetBlockStructureAsync(document, cancellationToken).ConfigureAwait(false);
            if (blockStructure == null)
            {
                return foldingRanges.ToArrayAndFree();
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var span in blockStructure.Spans)
            {
                if (!span.IsCollapsible)
                {
                    continue;
                }

                var linePositionSpan = text.Lines.GetLinePositionSpan(span.TextSpan);
                foldingRanges.Add(new FoldingRange()
                {
                    StartLine = linePositionSpan.Start.Line,
                    StartCharacter = linePositionSpan.Start.Character,
                    EndLine = linePositionSpan.End.Line,
                    EndCharacter = linePositionSpan.End.Character,
                    Kind = ConvertToWellKnownBlockType(span.Type),
                });
            }

            return foldingRanges.ToArrayAndFree();

            // local functions
            // TODO - Figure out which blocks should be returned as a folding range (and what kind).
            // https://github.com/dotnet/roslyn/projects/45#card-20049168
            static string ConvertToWellKnownBlockType(string kind)
            {
                switch (kind)
                {
                    case BlockTypes.Comment: return FoldingRangeKind.Comment;
                    case BlockTypes.Imports: return FoldingRangeKind.Imports;
                    case BlockTypes.PreprocessorRegion: return FoldingRangeKind.Region;
                    default: return null;
                }
            }
        }
    }
}
