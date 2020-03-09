using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public static class EnumExtensionMethods
	{
		static Dictionary<(Type, object), string> _dic = new Dictionary<(Type, object), string>();

		public static string GetDescription(this object enumValue) // where T : struct
		{
			if (enumValue is null)
			{
				throw new ArgumentNullException(nameof(enumValue));
			}
			// var type = typeof(T);
			var type = enumValue.GetType();

			if (!_dic.TryGetValue((type, enumValue), out var known))
			{
				_dic[(type, enumValue)] = known = GetDescriptionCore(enumValue);
			}

			return known;
		}

		static string GetDescriptionCore(object enumValue)
		{
			// var type = typeof(T);
			var type = enumValue.GetType();

			var str = enumValue.ToString();
			var memberInfo = type.GetMember(str);

			if ((memberInfo != null && memberInfo.Length == 1))
			{
				var attribs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
				if (attribs != null && attribs.Length > 0)
				{
					return ((DescriptionAttribute)attribs[0]).Description;
				}
				else
				{
					var sb = new StringBuilder(str);
					var seenWordToBreak = false;
					var inTheMiddleOfWord = false;
					var wordStart = 0;
					for (var i = 1; i < sb.Length; i++)
					{
						if (char.IsUpper(sb[i]) && seenWordToBreak)
						{
							sb.Insert(i, ' ');
							i++;
							seenWordToBreak = false;
							inTheMiddleOfWord = false;
							wordStart = i;
							// sb[i] = char.ToLowerInvariant(sb[i]);
						}

						if (char.IsLower(sb[i])) {
							if (wordStart + 1 == i) // This is a usual word
							{
								seenWordToBreak = true;
								inTheMiddleOfWord = true;
								if (i > 1)
								{
									sb[i - 1] = char.ToLowerInvariant(sb[i - 1]);
								}
							}

							if (wordStart + 1 < i && !inTheMiddleOfWord) // GenericSOCKSFailure
							{
								Debug.Assert(char.IsUpper(sb[wordStart]));
								Debug.Assert(char.IsUpper(sb[wordStart + 1]));

								if (i - wordStart == 2 && wordStart > 0)
								{
									// previous word was one-letter: ThisIsAHome
									sb[wordStart] = char.ToLowerInvariant(sb[wordStart]);
								}

								sb.Insert(i - 1, ' ');
								sb[i] = char.ToLowerInvariant(sb[i]);
								wordStart = i;
								i++;
								inTheMiddleOfWord = true;
								seenWordToBreak = true;
							}
						}
					}
					str = sb.ToString();
				}
			}
			return str;
		}

	}
}
