/*
* libc/stdlib/malloc/malloc.c -- malloc function
*
*  Copyright (C) 2002,03  NEC Electronics Corporation
*  Copyright (C) 2002,03  Miles Bader <miles@gnu.org>
*
* This file is subject to the terms and conditions of the GNU Lesser
* General Public License.  See the file COPYING.LIB in the main
* directory of this archive for more details.
*
* Written by Miles Bader <miles@gnu.org>
*/

#include "malloc.h"
#include "heap.h"

/* The malloc heap.  We provide a bit of initial static space so that
programs can do a little mallocing without mmaping in more space.  */
//HEAP_DECLARE_STATIC_FREE_AREA (initial_fa, 256);
//struct heap_free_area *__malloc_heap = HEAP_INIT_WITH_FA (initial_fa);
//static struct heap_free_area g_malloc_heap = {0,0,0};
struct heap_free_area * __malloc_heap = 0;

#if defined(CLR_MALLOC_HEAP_SIZE_AND_POINT)
void * __malloc_heap_point = 0;
size_t __malloc_heap_size = 0;
#endif


static void *
__malloc_from_heap (size_t size, struct heap_free_area **heap)
{
	void *mem;

	/* Include extra space to record the size of the allocated block.  */
	size += MALLOC_HEADER_SIZE;

	//__heap_lock (heap_lock);

	/* First try to get memory that's already in our heap.  */
	mem = __heap_alloc (heap, &size);

	//__heap_unlock (heap_lock);

	if (likely (mem))
		/* Record the size of the block and get the user address.  */
	{
		mem = MALLOC_SETUP (mem, size);
	}

	return mem;
}

static void
__free_to_heap (void *mem, struct heap_free_area **heap)
{
	size_t size;
	struct heap_free_area *fa;

	/* Check for special cases.  */
	if (unlikely (! mem))
		return;

	/* Normal free.  */

	size = MALLOC_SIZE (mem);
	mem = MALLOC_BASE (mem);

	__heap_lock (heap_lock);

	/* Put MEM back in the heap, and get the free-area it was placed in.  */
	fa = __heap_free (heap, mem, size);

	__heap_unlock(heap_lock);
}

//////////////////////////////////////////////////////////////////////////////////////////////////////

void 
crt_heap_init(void * heap_start,size_t size)
{
	unsigned int ui_start = (unsigned int)heap_start;
	unsigned int ui_end = ui_start + size;

	if(!heap_start || !size || (size < HEAP_MIN_SIZE + MALLOC_ALIGNMENT))
		return ;

	/*heap_start not alignment*/
	if(ui_start & (MALLOC_ALIGNMENT - 1)){
		ui_start = HEAP_ADJUST_SIZE(ui_start);
		size = ui_end - ui_start;
		ui_end = ui_start + size;
		heap_start = (void*)ui_start;
	}

	/*alignment size*/
	if(size & (MALLOC_ALIGNMENT - 1))
		size = HEAP_ADJUST_SIZE(size - MALLOC_ALIGNMENT);
	
	if(ui_end & (MALLOC_ALIGNMENT - 1))
		ui_end = HEAP_ADJUST_SIZE(ui_end - HEAP_MIN_SIZE - MALLOC_ALIGNMENT);
	else
		ui_end -= HEAP_MIN_SIZE;
	
	__malloc_heap = (struct heap_free_area*)ui_end;
	memset(__malloc_heap,0,sizeof(struct heap_free_area));


	/* Put BLOCK into the heap.  */
	__heap_free (&__malloc_heap, heap_start, ui_end - ui_start);
}

void *
crt_malloc (size_t size)
{
	if(!size) return 0;

	return __malloc_from_heap (size, &__malloc_heap);
}

void
crt_free (void *mem)
{
	__free_to_heap (mem, &__malloc_heap);
}

void *
crt_realloc (void *mem, size_t new_size)
{
	size_t size;
	char *base_mem;

	/* Check for special cases.  */
	if (! new_size)
	{
		crt_free (mem);
		return crt_malloc (new_size);
	}
	if (! mem)
		return crt_malloc (new_size);
	/* This matches the check in malloc() */
/*	if (unlikely(((unsigned long)new_size > (unsigned long)(MALLOC_HEADER_SIZE*-2))))
		return NULL;*/

	/* Normal realloc.  */

	base_mem = MALLOC_BASE (mem);
	size = MALLOC_SIZE (mem);

	/* Include extra space to record the size of the allocated block.
	Also make sure that we're dealing in a multiple of the heap
	allocation unit (SIZE is already guaranteed to be so).*/
	new_size = HEAP_ADJUST_SIZE (new_size + MALLOC_HEADER_SIZE);

	if (new_size < sizeof (struct heap_free_area))
		/* Because we sometimes must use a freed block to hold a free-area node,
		we must make sure that every allocated block can hold one.  */
		new_size = HEAP_ADJUST_SIZE (sizeof (struct heap_free_area));

	if (new_size > size)
		/* Grow the block.  */
	{
		size_t extra = new_size - size;

		__heap_lock (&__malloc_heap_lock);
		extra = __heap_alloc_at (&__malloc_heap, base_mem + size, extra);
		__heap_unlock (&__malloc_heap_lock);

		if (extra)
			/* Record the changed size.  */
			MALLOC_SET_SIZE (base_mem, size + extra);
		else
			/* Our attempts to extend MEM in place failed, just
			allocate-and-copy.  */
		{
			void *new_mem = crt_malloc (new_size - MALLOC_HEADER_SIZE);
			if (new_mem)
			{
				crt_memcpy (new_mem, mem, size - MALLOC_HEADER_SIZE);
				crt_free (mem);
			}
			mem = new_mem;
		}
	}
	else if (new_size + MALLOC_REALLOC_MIN_FREE_SIZE <= size)
		/* Shrink the block.  */
	{
		__heap_lock (&__malloc_heap_lock);
		__heap_free (&__malloc_heap, base_mem + new_size, size - new_size);
		__heap_unlock (&__malloc_heap_lock);
		MALLOC_SET_SIZE (base_mem, new_size);
	}

	return mem;
}


void * crt_calloc(size_t nmemb, size_t lsize)
{
	void *result;
	size_t size=lsize * nmemb;

	/* guard vs integer overflow, but allow nmemb
	* to fall through and call malloc(0) */
	if (nmemb && lsize != (size / nmemb)) 
		return 0;

	if ((result=crt_malloc(size)) != NULL) {
		crt_memset(result, 0, size);
	}
	return result;
}
