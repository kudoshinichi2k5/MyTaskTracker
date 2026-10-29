/* AdminPortal Comment Page Component - Full Sync with Task & Project Pages */
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface Comment {
  id: number;
  content: string;
  authorName: string;
  createdAt: Date;
}

@Component({
  selector: 'app-comment-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './comment-page.component.html',
  styleUrls: ['./comment-page.component.css']
})
export class CommentPageComponent implements OnInit {
  comments: Comment[] = [];

  ngOnInit(): void {
    // Fetch comments - same pattern as TaskService
    // this.commentService.getComments().subscribe({
    //   next: (comments) => this.comments = comments,
    //   error: (err) => console.error('Failed to load comments', err)
    // });
  }
}