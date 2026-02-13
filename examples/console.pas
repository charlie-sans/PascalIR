program ConsoleExample;
uses SysUtils;
var
    input: string;
    cmd: string;
    arg: string;
    i: integer;
begin
    WriteLn('Hello, World!');
    while true do 
    begin
        Write('Enter a command (type "exit" to quit): ');
        ReadLn(input);
        if input = 'exit' then
        begin
            WriteLn('Goodbye!');
            Break;
        end
        else if input = 'help' then
        begin
            WriteLn('Available commands:');
            WriteLn('  help - Show this help message');
            WriteLn('  exit - Exit the program');
            WriteLn('  about - Show program information');
            WriteLn('  cls - Clear the screen');
            WriteLn('  demo - Run a small demo loop');
            WriteLn('  echo <text> - Echo the provided text');
        end
        else if input = 'about' then
        begin
            WriteLn('PascalIR Console Example - Extended');
            WriteLn('This demo shows loops, conditionals, and procedure calls.');
        end
        else if input = 'cls' then
        begin
            
            WriteLn('\x1b[2J\x1b[H'); // ANSI escape code to clear screen and move cursor to home
        end
        else if input = 'demo' then
        begin
            WriteLn('Demo: counting 1 to 5');
            for i := 1 to 5 do
            begin
                WriteLn('Demo iteration: ', i);
            end;
            WriteLn('Demo complete.');
        end
        else if input = 'echo' then
        begin
            Write('Echo text: ');
            ReadLn(arg);
            WriteLn(arg);
        end
        else
            WriteLn('You entered: ', input);
    end;

end.