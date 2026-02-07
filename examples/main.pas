var
  i, j, k: Integer;
begin
  WriteLn('Hello, World!');
  for i := 1 to 5 do
  begin
    WriteLn('This is line number ', i);
  for j := 1 to 3 do
  begin
    WriteLn('Nested loop iteration: ', j);
  end;
  end;
  for k := 1 to 2 do
  begin
    WriteLn('Innermost loop iteration: ', k);
  end;
  for i := 10 downto 1 do
  begin
    writeln('i = ', i);
    { other statements here }
  end;
end.