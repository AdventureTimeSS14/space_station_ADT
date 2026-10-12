#!/bin/bash
c=$1; shift
for f in "$@"; do
  git show "$c:$f" 2>/dev/null | sed '1s/^\xEF\xBB\xBF//' | tr -d '\r' > /tmp/rm_base
  git show "$c^:$f" 2>/dev/null | sed '1s/^\xEF\xBB\xBF//' | tr -d '\r' > /tmp/rm_theirs
  git show "HEAD:$f" | tr -d '\r' > /tmp/rm_res
  git merge-file --diff3 -L ours -L base -L theirs /tmp/rm_res /tmp/rm_base /tmp/rm_theirs
  echo "$? $f"
  cp /tmp/rm_res "$f"
done
