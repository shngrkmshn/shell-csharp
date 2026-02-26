
public class TokenizationHandler
{

    //unless stuff gets really complicated, this will handle every possible input "rule" 
    //POSSIBLE BUG: only a single quote is in the entire string
    public static List<string>? Tokenize(string? input)
    {
        //should not be possible to get empty string, anyhow
        if (input == null)
            return null;
        
        var tokens = new List<string>();
        var currentToken = new System.Text.StringBuilder();
        bool inSingleQuote = false, inDoubleQuote = false; 
        bool backSlashed = false, backSlashedInDoubleQuote = false; //this is way too specific of a bool probably
        
        foreach (var character in input)
        {
            //escape
            if (backSlashed)
            {
                backSlashed = false;
                currentToken.Append(character);
                continue;
            } 
            //escape 2.0
            if (backSlashedInDoubleQuote) //you have been slashed baby
            {
                backSlashedInDoubleQuote = false;
                switch (character)
                {
                    case '"' or '\\':
                        currentToken.Append(character);
                        continue;
                    default: //no special escape, we add the backslash to the string, no edge cases possible I think
                        currentToken.Append('\\');
                        currentToken.Append(character);
                        continue; 
                }
            }
            
            //To add: early outs for skipper characters
            switch (character)
            {
                case '\'' when !inDoubleQuote && !backSlashed:
                    inSingleQuote = !inSingleQuote; 
                    continue;
                case '"' when !inSingleQuote  && !backSlashed:
                    inDoubleQuote = !inDoubleQuote; 
                    continue;
                case '\\' when !inDoubleQuote && !inSingleQuote && !backSlashed: //because backslash is already an escape character in windows
                    backSlashed = !backSlashed;
                    continue;
                case '\\' when inDoubleQuote:
                    backSlashedInDoubleQuote = !backSlashedInDoubleQuote;
                    continue;
            }

            if (char.IsWhiteSpace(character) && !inDoubleQuote && !inSingleQuote && !backSlashed && !backSlashedInDoubleQuote)
            {
                if (currentToken.Length <= 0) continue;
                tokens.Add(currentToken.ToString());
                currentToken.Clear();
                continue;
            }
            
            // Pipe indicator (only when not in quotes)
            if (character == '|' && !inSingleQuote && !inDoubleQuote)
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                }
                tokens.Add("|");
                continue;
            }
            //skip
                currentToken.Append(character);
        }
        if (currentToken.Length > 0)
            tokens.Add(currentToken.ToString());
        return tokens;
    }
}